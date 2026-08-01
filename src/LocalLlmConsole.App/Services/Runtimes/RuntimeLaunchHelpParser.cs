using System.Text.RegularExpressions;

namespace LocalLlmConsole.Services;

public static partial class RuntimeLaunchHelpParser
{
    public static IReadOnlyList<RuntimeLaunchOptionDefinition> Parse(string? helpText)
    {
        if (string.IsNullOrWhiteSpace(helpText)) return [];

        var definitions = new List<RuntimeLaunchOptionDefinition>();
        foreach (var rawLine in helpText.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = AnsiEscape().Replace(rawLine, "").TrimEnd();
            var nameMatches = OptionName().Matches(line).ToArray();
            var names = nameMatches.Select(match => match.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (names.Length == 0) continue;

            var firstName = line.IndexOf(names[0], StringComparison.Ordinal);
            if (firstName < 0 || line[..firstName].Any(character => !char.IsWhiteSpace(character))) continue;
            var lastNameEnd = nameMatches.Max(match => match.Index + match.Length);
            var descriptionStart = DescriptionGap().Match(line, lastNameEnd);
            var declaration = descriptionStart.Success ? line[firstName..descriptionStart.Index] : line[firstName..];
            var description = descriptionStart.Success ? line[(descriptionStart.Index + descriptionStart.Length)..].Trim() : "";
            var valueHint = ValueHint(declaration, names);
            var choices = ChoiceValues(valueHint, description);
            var primary = names.FirstOrDefault(name => name.StartsWith("--", StringComparison.Ordinal)) ?? names[0];
            var kind = InferValueKind(primary, valueHint, description, choices);
            definitions.Add(new RuntimeLaunchOptionDefinition(primary, names, valueHint, description, kind, choices, AdvertisedDefault($"{valueHint} {description}")));
        }

        return definitions
            .GroupBy(definition => definition.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(definition => definition.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string ValueHint(string declaration, IReadOnlyList<string> names)
    {
        var tail = declaration;
        foreach (var name in names)
            tail = tail.Replace(name, "", StringComparison.Ordinal);
        return tail.Trim(' ', ',', '=', '\t');
    }

    private static IReadOnlyList<string> ChoiceValues(string valueHint, string description)
    {
        var match = ChoiceGroup().Match(valueHint);
        if (!match.Success) match = ChoiceGroup().Match(description);
        if (!match.Success) return [];
        return match.Groups[1].Value.Split(['|', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string AdvertisedDefault(string description)
    {
        var match = DefaultValuePattern().Match(description);
        if (!match.Success) return "";
        return match.Groups["value"].Value.Trim().Trim('"', '\'');
    }

    private static RuntimeLaunchOptionValueKind InferValueKind(
        string name,
        string valueHint,
        string description,
        IReadOnlyList<string> choices)
    {
        if (string.IsNullOrWhiteSpace(valueHint)) return RuntimeLaunchOptionValueKind.Switch;
        if (choices.Count > 1) return RuntimeLaunchOptionValueKind.Choice;
        var semanticText = $"{name} {valueHint} {description}";
        if (name.EndsWith("-dir", StringComparison.OrdinalIgnoreCase)
            || semanticText.Contains("directory", StringComparison.OrdinalIgnoreCase)
            || semanticText.Contains("folder", StringComparison.OrdinalIgnoreCase))
            return RuntimeLaunchOptionValueKind.Directory;
        if (name.Contains("file", StringComparison.OrdinalIgnoreCase)
            || name.Contains("path", StringComparison.OrdinalIgnoreCase)
            || valueHint.Contains("FILE", StringComparison.OrdinalIgnoreCase)
            || valueHint.Contains("PATH", StringComparison.OrdinalIgnoreCase))
            return RuntimeLaunchOptionValueKind.File;
        return RuntimeLaunchOptionValueKind.Text;
    }

    [GeneratedRegex("\\x1B(?:[@-Z\\\\-_]|\\[[0-?]*[ -/]*[@-~])")]
    private static partial Regex AnsiEscape();

    [GeneratedRegex("(?<!\\S)-{1,2}[A-Za-z][A-Za-z0-9-]*")]
    private static partial Regex OptionName();

    [GeneratedRegex("\\s{2,}")]
    private static partial Regex DescriptionGap();

    [GeneratedRegex("[\\[<{(]([^\\]}>)]*[|,][^\\]}>)]*)[\\]}>)]")]
    private static partial Regex ChoiceGroup();

    [GeneratedRegex("(?i:\\bdefault(?:\\s+value)?\\s*(?::|=|\\bis\\b)\\s*)(?<value>\"[^\"]*\"|'[^']*'|[^\\s,;)\\]]+)")]
    private static partial Regex DefaultValuePattern();
}
