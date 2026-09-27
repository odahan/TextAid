namespace TextAid.Core;

/// <summary>Writes opt-in diagnostic metadata without retaining user content or secrets.</summary>
public sealed class DebugSessionLog : IDisposable
{
    private readonly StreamWriter? writer;
    private readonly bool fullLogEnabled;

    private DebugSessionLog(StreamWriter? writer, string? path, bool fullLogEnabled)
    {
        this.writer = writer;
        Path = path;
        this.fullLogEnabled = fullLogEnabled;
    }

    /// <summary>Gets the current debug-log path, or <see langword="null"/> when diagnostics are disabled.</summary>
    public string? Path { get; }

    /// <summary>Starts a new debug session, truncating its single text log only when diagnostics are enabled.</summary>
    public static DebugSessionLog Start(bool enabled, bool fullLogEnabled, string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (!enabled) return new DebugSessionLog(null, null, false);

        Directory.CreateDirectory(directory);
        string path = System.IO.Path.Combine(directory, "TextAid.debug.log");
        var writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
        var log = new DebugSessionLog(writer, path, fullLogEnabled);
        log.Write("debug-session-started", ("fullLog", fullLogEnabled));
        return log;
    }

    /// <summary>Appends a complete diagnostic text field only when Full log is explicitly enabled.</summary>
    public void WriteFullText(string eventName, string? content)
    {
        if (writer is null || !fullLogEnabled || string.IsNullOrEmpty(content)) return;
        writer.WriteLine($"{DateTimeOffset.UtcNow:O} event={eventName} text={RedactCredentials(EscapeText(content))}");
    }

    /// <summary>Appends a technical event name and safe numeric or categorical metadata.</summary>
    public void Write(string eventName, params (string Name, object? Value)[] metadata)
    {
        if (writer is null) return;
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        string details = string.Join(" ", metadata.Select(item => $"{item.Name}={FormatMetadata(item.Value)}"));
        writer.WriteLine($"{DateTimeOffset.UtcNow:O} event={eventName}{(details.Length == 0 ? string.Empty : " " + details)}");
    }

    /// <summary>Records safe exception diagnostics while excluding untrusted exception messages and data.</summary>
    public void WriteException(string operation, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (writer is null) return;

        int depth = 0;
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            string prefix = depth == 0 ? "exception" : "inner-exception";
            writer.WriteLine($"{DateTimeOffset.UtcNow:O} event={prefix} operation={FormatOperation(operation)} depth={depth} type={current.GetType().FullName ?? current.GetType().Name} hresult={current.HResult}");
            WriteExceptionCodes(current, operation, depth);
            if (fullLogEnabled)
                writer.WriteLine($"{DateTimeOffset.UtcNow:O} event=exception-message operation={FormatOperation(operation)} depth={depth} text={RedactCredentials(EscapeText(current.Message))}");
            WriteStackTrace(current.StackTrace, operation, depth);
            depth++;
        }
    }

    /// <inheritdoc />
    public void Dispose() => writer?.Dispose();

    private static string FormatMetadata(object? value) => value switch
    {
        null => "null",
        bool boolean => boolean ? "true" : "false",
        byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!,
        ConnectionCategory category => category.ToString(),
        UserFacingFailure failure => failure.ToString(),
        _ => "[redacted]"
    };

    private void WriteExceptionCodes(Exception exception, string operation, int depth)
    {
        if (exception is System.Net.Http.HttpRequestException http && http.StatusCode is not null)
            writer!.WriteLine($"{DateTimeOffset.UtcNow:O} event=exception-http-status operation={FormatOperation(operation)} depth={depth} status={(int)http.StatusCode.Value}");
        if (exception is System.Net.Sockets.SocketException socket)
            writer!.WriteLine($"{DateTimeOffset.UtcNow:O} event=exception-socket operation={FormatOperation(operation)} depth={depth} code={(int)socket.SocketErrorCode}");
        if (exception is System.ComponentModel.Win32Exception win32)
            writer!.WriteLine($"{DateTimeOffset.UtcNow:O} event=exception-win32 operation={FormatOperation(operation)} depth={depth} code={win32.NativeErrorCode}");
        if (exception is System.Text.Json.JsonException json)
            writer!.WriteLine($"{DateTimeOffset.UtcNow:O} event=exception-json operation={FormatOperation(operation)} depth={depth} line={json.LineNumber ?? -1} position={json.BytePositionInLine ?? -1}");
    }

    private void WriteStackTrace(string? stackTrace, string operation, int depth)
    {
        if (string.IsNullOrWhiteSpace(stackTrace)) return;
        string normalized = stackTrace.ReplaceLineEndings(" | ").Trim();
        if (normalized.Length > 12000) normalized = normalized[..12000] + "…";
        writer!.WriteLine($"{DateTimeOffset.UtcNow:O} event=exception-stack operation={FormatOperation(operation)} depth={depth} trace={normalized}");
    }

    private static string FormatOperation(string operation) =>
        System.Text.RegularExpressions.Regex.IsMatch(operation, "^[a-z0-9-]+$", System.Text.RegularExpressions.RegexOptions.CultureInvariant)
            ? operation
            : "[redacted]";

    private static string EscapeText(string value) => value.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);

    private static string RedactCredentials(string value)
    {
        string redacted = System.Text.RegularExpressions.Regex.Replace(value, "(?i)(bearer\\s+)[^\\s,;]+", "$1[redacted]");
        redacted = System.Text.RegularExpressions.Regex.Replace(redacted, "(?i)([\\\"']?(?:api[_ -]?key|token|secret|password|authorization)[\\\"']?\\s*[:=]\\s*[\\\"']?)[^\\s,;\\\"']+", "$1[redacted]");
        redacted = System.Text.RegularExpressions.Regex.Replace(redacted, "(?i)\\bsk-[a-z0-9_-]+\\b", "[redacted-api-key]");
        return redacted;
    }
}
