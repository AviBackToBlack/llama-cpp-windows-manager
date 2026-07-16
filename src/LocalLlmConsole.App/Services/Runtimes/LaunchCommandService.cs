using System.Globalization;

namespace LocalLlmConsole.Services;

/// <summary>Builds and parses llama-server command lines from strongly-typed launch options.</summary>
public static class LaunchCommandService
{
    private static readonly string[] DisallowedFlags = ["--host", "--port", "--api-key"];

    // The model flag is mandatory and universal to every llama-server build, so it must never
    // be dropped by runtime capability filtering (a --help parse miss must not launch modelless).
    private static readonly string[] EssentialFlags = ["--model", "-m"];

    /// <summary>
    /// Builds the command line for llama-server as a display string.
    /// If <see cref="LlamaServerLaunchOptions.SupportedFlags"/> is set, known flags that the selected runtime does not advertise in --help are omitted.
    /// The preview path uses SupportedFlags = null so the user sees the full command they asked for; RuntimeAdapter filters the same set before launching.
    /// Values are quoted for display so the command can round-trip through the parser; use <see cref="BuildCommandTokens"/> for raw process arguments.
    /// </summary>
    public static string BuildCommand(LlamaServerLaunchOptions options)
        => string.Join(" ", BuildCommandTokens(options).Select(QuoteIfNeeded));

    /// <summary>
    /// Builds the raw, unquoted argument tokens for llama-server without string round-tripping.
    /// Callers that feed the tokens into <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList"/> (e.g. <see cref="RuntimeAdapter.BuildArgs"/>)
    /// must use this instead of parsing the string returned by <see cref="BuildCommand"/>, because ArgumentList applies OS-level quoting itself.
    /// </summary>
    public static IReadOnlyList<string> BuildCommandTokens(LlamaServerLaunchOptions options)
    {
        var flagValues = BuildFlagValues(options);
        var tokens = new List<string>();

        foreach (var schemaFlag in LlamaServerFlagSchema.All)
        {
            var flagKey = schemaFlag.Names.FirstOrDefault(n => flagValues.ContainsKey(n));
            if (string.IsNullOrWhiteSpace(flagKey)) continue;
            if (!flagValues.TryGetValue(flagKey, out var value) || string.IsNullOrWhiteSpace(value)) continue;

            if (DisallowedFlags.Contains(flagKey, StringComparer.OrdinalIgnoreCase))
                continue;

            if (schemaFlag.ValueType == FlagValueType.Boolean)
            {
                if (IsTruthyBoolean(value))
                {
                    if (!IsDefaultValue(schemaFlag, value) && IsSupportedByRuntime(schemaFlag, flagKey, options.SupportedFlags))
                        tokens.Add(flagKey);
                }
                else if (IsFalsyBoolean(value))
                {
                    if (IsDefaultValue(schemaFlag, value)) continue;
                    var negatedToken = flagKey.StartsWith("--no-", StringComparison.OrdinalIgnoreCase)
                        ? flagKey
                        : "--no-" + flagKey[2..];
                    if (IsSupportedByRuntime(schemaFlag, negatedToken, options.SupportedFlags))
                        tokens.Add(negatedToken);
                }
                else if (string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase))
                {
                    // omit; default value
                }
            }
            else
            {
                if (!IsDefaultValue(schemaFlag, value) && IsSupportedByRuntime(schemaFlag, flagKey, options.SupportedFlags))
                {
                    tokens.Add(flagKey);
                    tokens.Add(value);
                }
            }
        }

        return tokens;
    }

    public static LaunchCommandParseResult ParseCommand(string commandLine, IReadOnlySet<string>? supportedFlags = null)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            return LaunchCommandParseResult.Empty;

        var tokens = CustomLaunchParameterParser.Parse(commandLine);
        var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var extraArgs = new List<string>();
        var errors = new List<string>();
        var securityWarnings = new List<string>();
        var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var i = 0;

        while (i < tokens.Count)
        {
            var token = tokens[i];
            if (TryGetSecurityWarning(token, out var warning))
            {
                securityWarnings.Add(warning);
                i += ConsumeSecurityCriticalFlagValue(tokens, i);
                continue;
            }

            if (!IsFlag(token))
            {
                extraArgs.Add(token);
                i++;
                continue;
            }

            var (flagName, flagValue, valueProvided, consumed) = ParseFlagToken(tokens, i);
            if (consumed == 0)
            {
                extraArgs.Add(token);
                i++;
                continue;
            }

            var (schemaFlag, keyName) = ResolveFlag(flagName);
            if (schemaFlag is null || (supportedFlags is not null && !IsSupportedByRuntime(schemaFlag, flagName, supportedFlags)))
            {
                extraArgs.Add(token);
                if (valueProvided && i + 1 < i + consumed)
                {
                    for (var j = i + 1; j < i + consumed; j++) extraArgs.Add(tokens[j]);
                }
                i += consumed;
                continue;
            }

            if (schemaFlag.ValueType == FlagValueType.Boolean && valueProvided && flagValue is not null && !IsBooleanString(flagValue))
            {
                consumed = 1;
                valueProvided = false;
                flagValue = null;
            }

            if (seen.TryGetValue(schemaFlag.PrimaryName, out var oldKey))
            {
                flags.Remove(oldKey);
            }
            seen[schemaFlag.PrimaryName] = keyName;

            if (schemaFlag.ValueType == FlagValueType.Boolean)
            {
                var isNegation = flagName.StartsWith("--no-", StringComparison.OrdinalIgnoreCase)
                    && !flagName.Equals(schemaFlag.PrimaryName, StringComparison.OrdinalIgnoreCase);
                var (boolValue, boolError) = ParseBooleanValue(flagName, flagValue, valueProvided, schemaFlag, isNegation);
                if (!string.IsNullOrEmpty(boolError))
                {
                    errors.Add(boolError);
                }
                else
                {
                    flags[keyName] = boolValue;
                }
            }
            else
            {
                if (!valueProvided)
                {
                    errors.Add($"Flag '{schemaFlag.PrimaryName}' requires a value.");
                }
                else
                {
                    flags[keyName] = flagValue ?? "";
                }
            }

            i += consumed;
        }

        var validation = LaunchCommandValidator.Validate(flags, validateFilePaths: false);
        errors.AddRange(validation.Errors);

        return new LaunchCommandParseResult(
            flags.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase),
            extraArgs,
            errors,
            securityWarnings);
    }

    private static bool TryGetSecurityWarning(string token, out string warning)
    {
        warning = "";
        if (string.IsNullOrWhiteSpace(token)) return false;

        foreach (var disallowed in DisallowedFlags)
        {
            if (string.Equals(token, disallowed, StringComparison.OrdinalIgnoreCase)
                || token.StartsWith(disallowed + "=", StringComparison.OrdinalIgnoreCase))
            {
                warning = $"Security-critical flag '{token}' is not allowed.";
                return true;
            }
        }

        return false;
    }

    private static int ConsumeSecurityCriticalFlagValue(IReadOnlyList<string> tokens, int i)
    {
        if (tokens[i].Contains('=', StringComparison.Ordinal)) return 1;
        if (i + 1 < tokens.Count && !IsFlag(tokens[i + 1])) return 2;
        return 1;
    }

    private static bool IsFlag(string token)
        => !string.IsNullOrWhiteSpace(token) && token.StartsWith('-') && token.Length > 1 && !char.IsDigit(token[1]);

    private static (string FlagName, string? Value, bool ValueProvided, int Consumed) ParseFlagToken(IReadOnlyList<string> tokens, int i)
    {
        var token = tokens[i];
        var equals = token.IndexOf('=', StringComparison.Ordinal);
        if (equals > 0)
        {
            return (token[..equals], token[(equals + 1)..], true, 1);
        }

        if (i + 1 < tokens.Count && !IsFlag(tokens[i + 1]))
        {
            return (token, tokens[i + 1], true, 2);
        }

        return (token, null, false, 1);
    }

    private static (LlamaServerFlag? Flag, string KeyName) ResolveFlag(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return (null, token);

        if (token.StartsWith("--no-", StringComparison.OrdinalIgnoreCase))
        {
            var positive = "--" + token[5..];
            var positiveFlag = LlamaServerFlagSchema.FindByName(positive);
            if (positiveFlag?.ValueType == FlagValueType.Boolean)
                return (positiveFlag, positiveFlag.PrimaryName);
        }

        var flag = LlamaServerFlagSchema.FindByName(token);
        var keyName = flag is null
            ? token
            : flag.Names.FirstOrDefault(n => n.Equals(token, StringComparison.OrdinalIgnoreCase)) ?? flag.PrimaryName;
        return (flag, keyName);
    }

    private static (string Value, string? Error) ParseBooleanValue(string token, string? value, bool valueProvided, LlamaServerFlag flag, bool isNegation)
    {
        if (isNegation)
        {
            if (!valueProvided)
                return ("false", null);

            var v = value?.Trim() ?? "";
            if (IsTruthyBoolean(v)) return ("false", null);
            if (IsFalsyBoolean(v)) return ("true", null);
            if (string.Equals(v, "auto", StringComparison.OrdinalIgnoreCase)) return (GetBooleanDefault(flag), null);
            return ("", $"Invalid boolean value '{v}' for flag '{flag.PrimaryName}'.");
        }

        if (!valueProvided)
            return ("true", null);

        var val = value?.Trim() ?? "";
        if (IsTruthyBoolean(val)) return ("true", null);
        if (IsFalsyBoolean(val)) return ("false", null);
        if (string.Equals(val, "auto", StringComparison.OrdinalIgnoreCase)) return (GetBooleanDefault(flag), null);

        return ("", $"Invalid boolean value '{val}' for flag '{flag.PrimaryName}'.");
    }

    private static string GetBooleanDefault(LlamaServerFlag flag)
        => flag.Default is true or "true" or "on" ? "true" : flag.Default is "auto" ? "auto" : "false";

    private static bool IsDefaultValue(LlamaServerFlag flag, string value)
    {
        if (flag.Default is null) return false;

        if (flag.ValueType == FlagValueType.Boolean)
        {
            var defaultValue = flag.Default.ToString() ?? "";
            if (string.Equals(value, defaultValue, StringComparison.OrdinalIgnoreCase)) return true;
            if (IsTruthyBoolean(value) && IsTruthyBoolean(defaultValue)) return true;
            if (IsFalsyBoolean(value) && IsFalsyBoolean(defaultValue)) return true;
            return string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase)
                && string.Equals(defaultValue, "auto", StringComparison.OrdinalIgnoreCase);
        }

        if (flag.ValueType is FlagValueType.Int or FlagValueType.Double
            && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var numericValue))
        {
            if (flag.Default is double d) return Math.Abs(numericValue - d) < 0.000_000_1;
            if (flag.Default is int i) return Math.Abs(numericValue - i) < 0.000_000_1;
            if (flag.Default is float f) return Math.Abs(numericValue - f) < 0.000_000_1;
        }

        return string.Equals(value, Convert.ToString(flag.Default, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTruthyBoolean(string value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);

    private static bool IsFalsyBoolean(string value)
        => string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "off", StringComparison.OrdinalIgnoreCase);

    private static bool IsBooleanString(string value)
        => IsTruthyBoolean(value) || IsFalsyBoolean(value) || string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase);

    private static bool IsSupportedByRuntime(LlamaServerFlag schemaFlag, string token, IReadOnlySet<string>? supportedFlags)
    {
        if (supportedFlags is null || supportedFlags.Count == 0) return true;
        if (schemaFlag.Names.Any(n => EssentialFlags.Contains(n, StringComparer.OrdinalIgnoreCase))) return true;
        if (supportedFlags.Contains(token)) return true;
        // For negated tokens (--no-*), don't fall back to the positive form
        if (token.StartsWith("--no-", StringComparison.OrdinalIgnoreCase))
            return false;
        if (schemaFlag.Names.Any(supportedFlags.Contains)) return true;
        return false;
    }

    private static string QuoteIfNeeded(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Any(c => char.IsWhiteSpace(c) || c == '"' || c == '\\'))
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
        return value;
    }

    private static Dictionary<string, string> BuildFlagValues(LlamaServerLaunchOptions options)
    {
        var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        AddFlag(flags, "--model", options.ModelPath);

        if (options.Backend is RuntimeBackend.Cuda or RuntimeBackend.Vulkan or RuntimeBackend.Metal or RuntimeBackend.Sycl)
            AddFlag(flags, "--n-gpu-layers", options.GpuLayers.ToString(CultureInfo.InvariantCulture));

        AddFlag(flags, "--ctx-size", options.ContextSize.ToString(CultureInfo.InvariantCulture));
        AddFlag(flags, "--parallel", options.ParallelSlots.ToString(CultureInfo.InvariantCulture));
        AddFlag(flags, "--batch-size", options.BatchSize.ToString(CultureInfo.InvariantCulture));
        AddFlag(flags, "--ubatch-size", options.MicroBatchSize.ToString(CultureInfo.InvariantCulture));
        AddBooleanFlag(flags, "--flash-attn", options.FlashAttention);
        AddFlag(flags, "--cache-type-k", options.CacheTypeK);
        AddFlag(flags, "--cache-type-v", options.CacheTypeV);

        AddBooleanFlag(flags, "--kv-offload", options.KvOffload);
        AddBooleanFlag(flags, "--kv-unified", options.KvUnified);

        AddPromptCacheFlags(flags, options.PromptCacheMode, options.PromptCacheRamMb);
        AddContextCheckpointFlags(flags, options.ContextCheckpointsMode, options.ContextCheckpointCount, options.ContextCheckpointEveryNTokens);

        AddBooleanFlag(flags, "--cont-batching", options.ContinuousBatching);

        AddFlagIfNotAuto(flags, "--reasoning", options.ReasoningMode);
        AddFlagIfNotAuto(flags, "--reasoning-format", options.ReasoningFormat);

        if (options.ReasoningBudget >= 0)
            AddFlag(flags, "--reasoning-budget", options.ReasoningBudget.ToString(CultureInfo.InvariantCulture));

        AddVisionFlags(flags, options);

        AddBooleanFlag(flags, "--jinja", options.JinjaMode);
        AddBooleanFlag(flags, "--mmap", options.MmapMode);

        if (string.Equals(options.MlockMode, "on", StringComparison.OrdinalIgnoreCase))
            AddFlag(flags, "--mlock", "true");

        AddFlag(flags, "--temp", options.Temperature.ToString("0.###", CultureInfo.InvariantCulture));
        AddFlag(flags, "--top-k", options.TopK.ToString(CultureInfo.InvariantCulture));
        AddFlag(flags, "--top-p", options.TopP.ToString("0.###", CultureInfo.InvariantCulture));
        AddFlag(flags, "--min-p", options.MinP.ToString("0.###", CultureInfo.InvariantCulture));
        AddFlag(flags, "--repeat-last-n", options.RepeatLastN.ToString(CultureInfo.InvariantCulture));
        AddFlag(flags, "--repeat-penalty", options.RepeatPenalty.ToString("0.###", CultureInfo.InvariantCulture));
        AddFlag(flags, "--presence-penalty", options.PresencePenalty.ToString("0.###", CultureInfo.InvariantCulture));
        AddFlag(flags, "--frequency-penalty", options.FrequencyPenalty.ToString("0.###", CultureInfo.InvariantCulture));

        if (options.MaxTokens >= 0)
            AddFlag(flags, "--predict", options.MaxTokens.ToString(CultureInfo.InvariantCulture));
        if (options.Seed >= 0)
            AddFlag(flags, "--seed", options.Seed.ToString(CultureInfo.InvariantCulture));
        if (options.Threads > 0)
            AddFlag(flags, "--threads", options.Threads.ToString(CultureInfo.InvariantCulture));

        AddFlagIfNotAuto(flags, "--rope-scaling", options.RopeScaling);
        if (options.RopeScale > 0)
            AddFlag(flags, "--rope-scale", options.RopeScale.ToString("0.###", CultureInfo.InvariantCulture));
        if (options.RopeFreqBase > 0)
            AddFlag(flags, "--rope-freq-base", options.RopeFreqBase.ToString("0.###", CultureInfo.InvariantCulture));
        if (options.RopeFreqScale > 0)
            AddFlag(flags, "--rope-freq-scale", options.RopeFreqScale.ToString("0.###", CultureInfo.InvariantCulture));

        AddSpeculativeFlags(flags, options);

        if (options.EnableMetrics)
            AddFlag(flags, "--metrics", "true");

        foreach (var kvp in options.FlagValues)
        {
            if (flags.ContainsKey(kvp.Key)) continue;
            AddFlag(flags, kvp.Key, kvp.Value);
        }

        return flags;
    }

    private static void AddFlag(Dictionary<string, string> flags, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        flags[name] = value;
    }

    private static void AddFlagIfNotAuto(Dictionary<string, string> flags, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase)) return;
        flags[name] = value;
    }

    private static void AddBooleanFlag(Dictionary<string, string> flags, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (string.Equals(value, "on", StringComparison.OrdinalIgnoreCase))
            flags[name] = "true";
        else if (string.Equals(value, "off", StringComparison.OrdinalIgnoreCase))
            flags[name] = "false";
    }

    private static void AddPromptCacheFlags(Dictionary<string, string> flags, string mode, int ramMb)
    {
        if (string.Equals(mode, "on", StringComparison.OrdinalIgnoreCase))
            flags["--cache-ram"] = ramMb.ToString(CultureInfo.InvariantCulture);
        else if (string.Equals(mode, "off", StringComparison.OrdinalIgnoreCase))
            flags["--cache-ram"] = "0";
    }

    private static void AddContextCheckpointFlags(Dictionary<string, string> flags, string mode, int count, int step)
    {
        if (string.Equals(mode, "on", StringComparison.OrdinalIgnoreCase))
        {
            flags["--ctx-checkpoints"] = count.ToString(CultureInfo.InvariantCulture);
            flags["--checkpoint-min-step"] = step.ToString(CultureInfo.InvariantCulture);
        }
        else if (string.Equals(mode, "off", StringComparison.OrdinalIgnoreCase))
        {
            flags["--ctx-checkpoints"] = "0";
        }
    }

    private static void AddVisionFlags(Dictionary<string, string> flags, LlamaServerLaunchOptions options)
    {
        if (string.Equals(options.VisionMode, "off", StringComparison.OrdinalIgnoreCase))
        {
            flags["--no-mmproj"] = "true";
            return;
        }

        if (!options.VisionProjectorEmbedded && !string.IsNullOrWhiteSpace(options.VisionProjectorPath))
            flags["--mmproj"] = options.VisionProjectorPath;

        if (options.VisionImageMinTokens > 0)
            flags["--image-min-tokens"] = options.VisionImageMinTokens.ToString(CultureInfo.InvariantCulture);
        if (options.VisionImageMaxTokens > 0)
            flags["--image-max-tokens"] = options.VisionImageMaxTokens.ToString(CultureInfo.InvariantCulture);
    }

    private static void AddSpeculativeFlags(Dictionary<string, string> flags, LlamaServerLaunchOptions options)
    {
        var speculativeType = LaunchSettingMetadataService.NormalizeSpeculativeType(options.SpeculativeType);
        if (string.Equals(speculativeType, "none", StringComparison.OrdinalIgnoreCase)) return;

        var llamaSpec = LaunchSettingMetadataService.LlamaSpeculativeTypeArgument(speculativeType);
        flags["--spec-type"] = llamaSpec;

        if (LaunchSettingMetadataService.IsAtomicMtpSpeculativeType(speculativeType))
        {
            if (!string.IsNullOrWhiteSpace(options.MtpHeadPath))
                flags["--mtp-head"] = options.MtpHeadPath;
            return;
        }

        if (speculativeType.StartsWith("draft-", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(options.SpecDraftModelPath))
                flags["--model-draft"] = options.SpecDraftModelPath;
            if (options.SpecDraftGpuLayers >= 0)
                flags["--n-gpu-layers-draft"] = options.SpecDraftGpuLayers.ToString(CultureInfo.InvariantCulture);
            if (options.SpecDraftMinTokens > 0)
                flags["--spec-draft-n-min"] = options.SpecDraftMinTokens.ToString(CultureInfo.InvariantCulture);
            if (options.SpecDraftMaxTokens > 0)
                flags["--spec-draft-n-max"] = options.SpecDraftMaxTokens.ToString(CultureInfo.InvariantCulture);
            if (options.SpecDraftPSplit >= 0)
                flags["--spec-draft-p-split"] = options.SpecDraftPSplit.ToString("0.###", CultureInfo.InvariantCulture);
            if (options.SpecDraftPMin >= 0)
                flags["--spec-draft-p-min"] = options.SpecDraftPMin.ToString("0.###", CultureInfo.InvariantCulture);
            flags["--cache-type-k-draft"] = options.SpecDraftCacheTypeK;
            flags["--cache-type-v-draft"] = options.SpecDraftCacheTypeV;
        }
    }
}
