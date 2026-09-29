namespace TextAid.Core;

/// <summary>Renders the deliberately small TextAid prompt-template language.</summary>
public static class TemplateRenderer
{
    /// <summary>Validates that a template contains only the supported text and answer placeholders.</summary>
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
            if (!placeholder.Equals("text", StringComparison.Ordinal) && !placeholder.Equals("answer", StringComparison.Ordinal))
                throw new InvalidOperationException($"Unsupported prompt placeholder '{{{{{placeholder}}}}}'.");
            position = close + 2;
        }
    }

    /// <summary>Renders text and an optional user answer without interpreting braces inside supplied values.</summary>
    public static string Render(string template, string text, string? answer = null)
    {
        ValidateTemplate(template);
        ArgumentNullException.ThrowIfNull(text);
        answer ??= string.Empty;
        var rendered = new System.Text.StringBuilder(template.Length + text.Length + answer.Length);
        int position = 0;
        while (position < template.Length)
        {
            int opening = template.IndexOf("{{", position, StringComparison.Ordinal);
            if (opening < 0)
            {
                rendered.Append(template, position, template.Length - position);
                break;
            }

            rendered.Append(template, position, opening - position);
            int close = template.IndexOf("}}", opening + 2, StringComparison.Ordinal);
            string placeholder = template[(opening + 2)..close].Trim();
            rendered.Append(placeholder.Equals("text", StringComparison.Ordinal) ? text : answer);
            position = close + 2;
        }

        return rendered.ToString();
    }

    /// <summary>Determines whether the template explicitly places the user answer.</summary>
    public static bool ContainsAnswerPlaceholder(string template)
    {
        ValidateTemplate(template);
        int position = 0;
        while (position < template.Length)
        {
            int opening = template.IndexOf("{{", position, StringComparison.Ordinal);
            if (opening < 0) return false;
            int close = template.IndexOf("}}", opening + 2, StringComparison.Ordinal);
            if (template[(opening + 2)..close].Trim().Equals("answer", StringComparison.Ordinal)) return true;
            position = close + 2;
        }

        return false;
    }
}
