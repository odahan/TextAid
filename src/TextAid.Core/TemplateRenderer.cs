namespace TextAid.Core;

/// <summary>Renders the deliberately small TextAid prompt-template language.</summary>
public static class TemplateRenderer
{
    /// <summary>Validates that a template contains only the supported text placeholder.</summary>
    public static void ValidateTemplate(string template)
    {
        ArgumentNullException.ThrowIfNull(template);
        int position = 0;
        while (position < template.Length)
        {
            int opening = template.IndexOf("{{", position, StringComparison.Ordinal);
            int unexpectedClosing = template.IndexOf("}}", position, StringComparison.Ordinal);
            if (unexpectedClosing >= 0 && (opening < 0 || unexpectedClosing < opening))
                throw new InvalidOperationException("A prompt template contains an unexpected closing placeholder.");
            if (opening < 0) break;

            int close = template.IndexOf("}}", opening + 2, StringComparison.Ordinal);
            if (close < 0) throw new InvalidOperationException("A prompt template contains an unclosed placeholder.");
            string placeholder = template[(opening + 2)..close].Trim();
            if (!placeholder.Equals("text", StringComparison.Ordinal)) throw new InvalidOperationException($"Unsupported prompt placeholder '{{{{{placeholder}}}}}'.");
            position = close + 2;
        }
    }

    /// <summary>Renders text once without interpreting braces inside the supplied text.</summary>
    public static string Render(string template, string text)
    {
        ValidateTemplate(template);
        ArgumentNullException.ThrowIfNull(text);
        return template.Replace("{{text}}", text, StringComparison.Ordinal);
    }
}
