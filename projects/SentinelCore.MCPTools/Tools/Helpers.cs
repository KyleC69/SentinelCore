// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         Helpers.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.Text.RegularExpressions;




namespace SentinelCoreMCP.Tools;





public static class Helpers
{



    /// <summary>
    ///     Splits raw multi-line tool output into a list of cleaned, single-line strings.
    ///     Line endings are normalized, empty lines are dropped, and every line is
    ///     stripped of control characters. The result is safe to return as a
    ///     structured payload (a JSON array of strings) without literal escape sequences.
    /// </summary>
    /// <param name="output">The raw output from an external tool or command.</param>
    /// <param name="maxLines">Optional maximum number of lines to return. Zero or negative means no limit.</param>
    /// <returns>The list of cleaned output lines.</returns>
    public static List<string> CleanOutputLines(string? output, int maxLines = 0)
    {
        List<string> lines = new();

        if (string.IsNullOrWhiteSpace(output))
        {
            return lines;
        }

        // Normalize line endings to '\n' before splitting.
        string normalized = output.Replace("\r\n", "\n").Replace('\r', '\n');

        foreach (string rawLine in normalized.Split('\n'))
        {
            string cleaned = CleanValue(rawLine);
            if (cleaned.Length > 0)
            {
                lines.Add(cleaned);

                if (maxLines > 0 && lines.Count >= maxLines)
                {
                    break;
                }
            }
        }

        return lines;
    }








    /// <summary>
    ///     Cleans a single string value for inclusion in a structured tool payload.
    ///     Strips control characters (including embedded CR/LF, NUL, and other
    ///     non-printable bytes) so the value survives JSON serialization without
    ///     producing literal escape sequences or control-character issues.
    ///     The value is returned unencoded and trimmed.
    /// </summary>
    /// <param name="value">The raw value, possibly containing control characters.</param>
    /// <returns>The cleaned, single-line string value.</returns>
    public static string CleanValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // Remove ANSI escape sequences first so their payload chars are not kept.
        string cleaned = Regex.Replace(value, @"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])", string.Empty);

        // Remove all control characters (C0, DEL, and C1 ranges). This includes
        // CR/LF, tabs, NUL, and other non-printable bytes that external tools
        // and registry values may embed.
        cleaned = new string(cleaned.Where(c => !char.IsControl(c)).ToArray());

        return cleaned.Trim();
    }








    public static string NormalizeToolOutput(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Remove ANSI escape sequences
        text = Regex.Replace(text, @"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])", string.Empty);

        // Remove non-useful control chars
        text = new string(text.Where(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t').ToArray());

        // Normalize line endings
        text = text.Replace("\r\n", Environment.NewLine);
        text = text.Replace("\n", Environment.NewLine);
        text = text.Replace("\r", Environment.NewLine);
        //text = text.Replace("\r\n", "\n").Replace('\r', '\n');

        // Trim only the surrounding whitespace. Do NOT route through CleanValue here:
        // CleanValue strips CR/LF/TAB, which would collapse the multi-line output this
        // method was written to normalize back into a single line.
        return text.Trim();
    }
}
