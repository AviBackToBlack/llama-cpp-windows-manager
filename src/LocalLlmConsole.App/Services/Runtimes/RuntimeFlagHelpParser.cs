using System.Text.RegularExpressions;

namespace LocalLlmConsole.Services;

/// <summary>Parses llama-server --help output to discover supported flags.</summary>
public static class RuntimeFlagHelpParser
{
    private static readonly Regex FlagTokenRegex = new(
        @"^--?[A-Za-z_][A-Za-z0-9_-]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex HeaderLineRegex = new(
        @"^\s*-----\s+.*\s+-----\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlySet<string> ParseSupportedFlags(string? helpText)
    {
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(helpText))
            return flags;

        try
        {
            var normalized = helpText
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n');

            foreach (var rawLine in normalized.Split('\n'))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (HeaderLineRegex.IsMatch(line)) continue;

                var tokens = line.Split([',', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                foreach (var token in tokens)
                {
                    if (FlagTokenRegex.IsMatch(token))
                    {
                        flags.Add(token);
                    }
                }
            }
        }
        catch
        {
            // Tolerant: return empty set on any parsing failure.
            flags.Clear();
        }

        return flags;
    }
}
