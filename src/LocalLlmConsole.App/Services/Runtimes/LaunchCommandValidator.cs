
namespace LocalLlmConsole.Services;

/// <summary>Validates llama-server flag values and cross-field rules.</summary>
public static class LaunchCommandValidator
{
    private static readonly string[] DisallowedFlags = ["--host", "--port", "--api-key"];

    public static ValidationResult Validate(IReadOnlyDictionary<string, string> flags, bool validateFilePaths = true)
    {
        var errors = new List<string>();

        foreach (var kvp in flags)
        {
            if (DisallowedFlags.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"Security-critical flag '{kvp.Key}' is not allowed in generic flag values.");
                continue;
            }

            var flag = LlamaServerFlagSchema.FindByName(kvp.Key);
            if (flag is null)
            {
                errors.Add($"Unknown flag '{kvp.Key}'.");
                continue;
            }

            var value = kvp.Value;
            if (string.IsNullOrWhiteSpace(value) && flag.ValueType != FlagValueType.Boolean)
            {
                errors.Add($"Flag '{flag.PrimaryName}' requires a value.");
                continue;
            }

            switch (flag.ValueType)
            {
                case FlagValueType.Boolean:
                    ValidateBoolean(flag, value, errors);
                    break;
                case FlagValueType.Int:
                    ValidateInt(flag, value, errors);
                    break;
                case FlagValueType.Double:
                    ValidateDouble(flag, value, errors);
                    break;
                case FlagValueType.Enum:
                    ValidateEnum(flag, value, errors);
                    break;
                case FlagValueType.File:
                    if (validateFilePaths) ValidateFile(flag, value, errors);
                    break;
                case FlagValueType.Path:
                    if (validateFilePaths) ValidatePath(flag, value, errors);
                    break;
                case FlagValueType.CommaList:
                    ValidateCommaList(flag, value, errors);
                    break;
            }
        }

        ValidateCrossFieldRules(flags, errors);

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }

    private static void ValidateBoolean(LlamaServerFlag flag, string value, List<string> errors)
    {
        var v = value?.Trim() ?? "";
        if (IsValidBooleanString(v)) return;
        errors.Add($"Flag '{flag.PrimaryName}' must be a boolean value (true, false, on, off, or auto).");
    }

    private static bool IsValidBooleanString(string value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "off", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase);

    private static void ValidateInt(LlamaServerFlag flag, string value, List<string> errors)
    {
        try
        {
            if (string.Equals(flag.PrimaryName, "--ctx-size", StringComparison.OrdinalIgnoreCase))
            {
                if (!LaunchSettingParser.TryNormalizeContextSize(value, out var contextSize) || contextSize < (int)(flag.Min ?? 0) || (flag.Max.HasValue && contextSize > flag.Max.Value))
                {
                    errors.Add($"{flag.PrimaryName} must be 0, a token count, or shorthand like 196k.");
                }
                return;
            }

            var v = LaunchSettingParser.ReadInt(value, flag.PrimaryName, (int)(flag.Min ?? int.MinValue), flag.Max is null ? null : (int)flag.Max.Value);
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(ex.Message);
        }
    }

    private static void ValidateDouble(LlamaServerFlag flag, string value, List<string> errors)
    {
        try
        {
            var v = LaunchSettingParser.ReadDouble(value, flag.PrimaryName, flag.Min ?? double.MinValue, flag.Max);
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(ex.Message);
        }
    }

    private static void ValidateEnum(LlamaServerFlag flag, string value, List<string> errors)
    {
        if (flag.AllowedValues is null || flag.AllowedValues.Count == 0) return;
        if (!flag.AllowedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"Flag '{flag.PrimaryName}' value '{value}' must be one of: {string.Join(", ", flag.AllowedValues)}.");
        }
    }

    private static void ValidateFile(LlamaServerFlag flag, string value, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        try
        {
            var path = Path.GetFullPath(value.Trim());
            if (!File.Exists(path))
            {
                errors.Add($"Flag '{flag.PrimaryName}' file '{value}' does not exist.");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Flag '{flag.PrimaryName}' file path '{value}' is invalid: {ex.Message}");
        }
    }

    private static void ValidatePath(LlamaServerFlag flag, string value, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        try
        {
            var path = Path.GetFullPath(value.Trim());
            if (!Directory.Exists(path))
            {
                errors.Add($"Flag '{flag.PrimaryName}' path '{value}' does not exist.");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Flag '{flag.PrimaryName}' path '{value}' is invalid: {ex.Message}");
        }
    }

    private static void ValidateCommaList(LlamaServerFlag flag, string value, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            errors.Add($"Flag '{flag.PrimaryName}' must contain at least one item.");
        }
    }

    private static void ValidateCrossFieldRules(IReadOnlyDictionary<string, string> flags, List<string> errors)
    {
        if (TryGetInt(flags, "--batch-size", out var batchSize) && TryGetInt(flags, "--ubatch-size", out var ubatchSize) && ubatchSize > batchSize)
        {
            errors.Add("--ubatch-size must be less than or equal to --batch-size.");
        }

        if (TryGetInt(flags, "--image-min-tokens", out var imageMin) && TryGetInt(flags, "--image-max-tokens", out var imageMax) && imageMax > 0 && imageMin > imageMax)
        {
            errors.Add("--image-min-tokens must be less than or equal to --image-max-tokens.");
        }
    }

    private static bool TryGetInt(IReadOnlyDictionary<string, string> flags, string name, out int value)
    {
        value = 0;
        if (!flags.TryGetValue(name, out var text)) return false;
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

}
