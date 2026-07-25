using System.Text.RegularExpressions;

namespace LocalLlmConsole.Services;

/// <summary>Parses llama-server --help output to discover supported flags.</summary>
public static class RuntimeFlagHelpParser
{
    private static readonly Regex FlagTokenRegex = new(
        @"^--?[A-Za-z_][A-Za-z0-9_\-\.]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex HeaderLineRegex = new(
        @"^\s*-----\s+.*\s+-----\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex DescriptionSeparatorRegex = new(
        @"(?: {2,}|\t+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlySet<string> ParseSupportedFlags(string? helpText)
    {
        // Short flags are case-sensitive; long-flag matching is handled by callers.
        var flags = new HashSet<string>(StringComparer.Ordinal);

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
                if (!line.StartsWith("-", StringComparison.Ordinal)) continue;

                // llama-server separates the option declaration from prose with aligned
                // whitespace. Only inspect that declaration so flag-like examples in the
                // description do not make unsupported capabilities look supported.
                Match? separator = null;
                foreach (Match candidate in DescriptionSeparatorRegex.Matches(line))
                {
                    var remainder = line[(candidate.Index + candidate.Length)..].TrimStart();
                    if (!remainder.StartsWith("-", StringComparison.Ordinal))
                    {
                        separator = candidate;
                        break;
                    }
                }

                var declaration = separator is not null ? line[..separator.Index] : line;
                var tokens = declaration.Split([',', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
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
