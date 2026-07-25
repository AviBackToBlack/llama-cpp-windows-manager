
namespace LocalLlmConsole.Services;

/// <summary>Validates llama-server flag values and cross-field rules.</summary>
public static class LaunchCommandValidator
{
    /// <param name="validateFilePaths">
    /// Validate existence in the current host file system. Command previews and runtime
    /// requests disable this because they may contain WSL paths or files created later.
    /// </param>
    public static ValidationResult Validate(IReadOnlyDictionary<string, string> flags, bool validateFilePaths = true)
    {
        var errors = new List<string>();

        foreach (var kvp in flags)
        {
            if (IsSecurityCriticalFlag(kvp.Key))
            {
                errors.Add($"Security-critical flag '{kvp.Key}' is not allowed in generic flag values.");
                continue;
            }

            var flag = LlamaServerFlagSchema.FindByName(kvp.Key);
            if (flag is null)
            {
                // Persistence deliberately preserves schema-unknown flags (see
                // LaunchCommandService.SanitizeFlagValues) so saved profiles survive schema
                // changes across app versions. Rejecting them here would make such a profile
                // unlaunchable and unsaveable with no UI control to clear the stale entry, so
                // tolerate them; they are emitted verbatim.
                continue;
            }

            var value = kvp.Value;
            ValidateValue(flag, value, errors, validateFilePaths);
        }

        ValidateCrossFieldRules(flags, errors);

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }

    internal static ValidationResult ValidateValue(LlamaServerFlag flag, string value, bool validateFilePaths = false)
    {
        var errors = new List<string>();
        ValidateValue(flag, value, errors, validateFilePaths);
        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors);
    }

    private static void ValidateValue(LlamaServerFlag flag, string value, List<string> errors, bool validateFilePaths)
    {
        if (string.IsNullOrWhiteSpace(value) && flag.ValueType != FlagValueType.Boolean)
        {
            errors.Add($"Flag '{flag.PrimaryName}' requires a value.");
            return;
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
            case FlagValueType.PathList:
                ValidatePathList(flag, value, errors, validateFilePaths, scaled: false);
                break;
            case FlagValueType.ScaledPathList:
                ValidatePathList(flag, value, errors, validateFilePaths, scaled: true);
                break;
            case FlagValueType.MultiToken:
                ValidateMultiToken(flag, value, errors);
                break;
        }
    }

    private static void ValidateBoolean(LlamaServerFlag flag, string value, List<string> errors)
    {
        var v = value?.Trim() ?? "";
        // LaunchCommandService owns the accepted spellings because it is the emission
        // authority; validating against the same predicate keeps the two from drifting.
        if (LaunchCommandService.IsBooleanString(v)) return;
        errors.Add($"Flag '{flag.PrimaryName}' must be a boolean value (true, false, on, off, or auto).");
    }

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

    private static bool IsSecurityCriticalFlag(string flagName)
        => LlamaServerFlagSchema.FindByName(flagName)?.IsSecurityCritical == true;

    private static void ValidatePathList(LlamaServerFlag flag, string value, List<string> errors, bool validateFilePaths, bool scaled)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var parts = value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length == 0 || parts.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add($"Flag '{flag.PrimaryName}' must not contain empty path entries.");
            return;
        }

        foreach (var part in parts)
        {
            var path = part.Trim();
            if (scaled)
            {
                var lastColon = part.LastIndexOf(':');
                var scaleText = lastColon >= 0 ? part[(lastColon + 1)..].Trim() : "";
                if (lastColon > 1 && double.TryParse(scaleText, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                {
                    path = part[..lastColon].Trim();
                }
                else
                {
                    errors.Add($"Flag '{flag.PrimaryName}' entry '{part}' is missing a valid scale suffix.");
                    continue;
                }
            }

            if (validateFilePaths)
            {
                if (Directory.Exists(path) || File.Exists(path)) continue;
                try
                {
                    var fullPath = Path.GetFullPath(path.Trim());
                    if (!Directory.Exists(fullPath) && !File.Exists(fullPath))
                        errors.Add($"Flag '{flag.PrimaryName}' path '{path}' does not exist.");
                }
                catch (Exception ex)
                {
                    errors.Add($"Flag '{flag.PrimaryName}' path '{path}' is invalid: {ex.Message}");
                }
            }
        }
    }

    private static void ValidateMultiToken(LlamaServerFlag flag, string value, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"Flag '{flag.PrimaryName}' requires {flag.Arity} values.");
            return;
        }

        var parts = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != flag.Arity)
        {
            errors.Add($"Flag '{flag.PrimaryName}' requires exactly {flag.Arity} values; got {parts.Length}.");
            return;
        }

        for (var i = 0; i < flag.Arity; i++)
        {
            if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                errors.Add($"Flag '{flag.PrimaryName}' value '{parts[i]}' is not a valid number.");
        }
    }

}
