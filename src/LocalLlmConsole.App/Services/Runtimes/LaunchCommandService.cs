using System.Globalization;

namespace LocalLlmConsole.Services;

/// <summary>Builds and parses llama-server command lines from strongly-typed launch options.</summary>
public static class LaunchCommandService
{
    // The model flag is mandatory and universal to every llama-server build, so it must never
    // be dropped by runtime capability filtering (a --help parse miss must not launch modelless).
    private static readonly string[] EssentialFlags = ["--model", "-m"];

    private static IEnumerable<string> SecurityCriticalFlagNames()
        => LlamaServerFlagSchema.All.Where(f => f.IsSecurityCritical).SelectMany(f => f.Names);

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
        => BuildCommandTokens(options, droppedFlags: null);

    /// <param name="droppedFlags">
    /// When set, receives the flag tokens that were omitted because the selected runtime does
    /// not support them, so callers can surface the divergence instead of dropping silently.
    /// </param>
    public static IReadOnlyList<string> BuildCommandTokens(LlamaServerLaunchOptions options, ICollection<string>? droppedFlags)
    {
        var firstClassValues = BuildFlagValues(options);
        var userValues = CanonicalizeFlagValues(options.FlagValues);
        var tokens = new List<string>();
        var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // The model path is mandatory and must always appear first.
        if (firstClassValues.TryGetValue("--model", out var modelPath) && !string.IsNullOrWhiteSpace(modelPath))
        {
            tokens.Add("--model");
            tokens.Add(modelPath);
            MarkEmitted(emitted, "--model");
        }

        var orderedFlagNames = options.FlagOrder is { Count: > 0 }
            ? options.FlagOrder
            : LlamaServerFlagSchema.All.Select(f => f.PrimaryName).ToList();

        foreach (var flagName in orderedFlagNames)
        {
            if (emitted.Contains(flagName)) continue;
            var schemaFlag = LlamaServerFlagSchema.FindByName(flagName);
            if (schemaFlag is null) continue;
            if (schemaFlag.IsSecurityCritical) continue;

            var firstClassKey = schemaFlag.Names.FirstOrDefault(n => firstClassValues.ContainsKey(n));
            if (!string.IsNullOrWhiteSpace(firstClassKey))
            {
                if (EmitFlag(
                    firstClassKey,
                    firstClassValues,
                    options.SupportedFlags,
                    tokens,
                    explicitValue: IsExplicitFirstClassValue(schemaFlag, firstClassKey, firstClassValues, options),
                    droppedFlags))
                    MarkEmitted(emitted, firstClassKey);
                continue;
            }

            var userKey = schemaFlag.Names.FirstOrDefault(n => userValues.ContainsKey(n));
            if (!string.IsNullOrWhiteSpace(userKey))
            {
                if (EmitFlag(userKey, userValues, options.SupportedFlags, tokens, explicitValue: true, droppedFlags))
                    MarkEmitted(emitted, userKey);
            }
        }

        foreach (var flagKey in firstClassValues.Keys)
        {
            if (emitted.Contains(flagKey)) continue;
            if (string.Equals(flagKey, "--model", StringComparison.OrdinalIgnoreCase)) continue;

            var schemaFlag = LlamaServerFlagSchema.FindByName(flagKey);
            if (schemaFlag?.IsSecurityCritical == true) continue;

            if (EmitFlag(
                flagKey,
                firstClassValues,
                options.SupportedFlags,
                tokens,
                explicitValue: schemaFlag is not null && IsExplicitFirstClassValue(schemaFlag, flagKey, firstClassValues, options),
                droppedFlags))
                MarkEmitted(emitted, flagKey);
        }

        foreach (var flagKey in userValues.Keys)
        {
            if (emitted.Contains(flagKey)) continue;
            if (string.Equals(flagKey, "--model", StringComparison.OrdinalIgnoreCase)) continue;

            var schemaFlag = LlamaServerFlagSchema.FindByName(flagKey);
            if (schemaFlag?.IsSecurityCritical == true) continue;

            if (EmitFlag(flagKey, userValues, options.SupportedFlags, tokens, explicitValue: true, droppedFlags))
                MarkEmitted(emitted, flagKey);
        }

        return tokens;
    }

    // A flag emitted under one name must count as emitted under every alias, otherwise a
    // first-class value registered under an alias (e.g. --model-draft) and a user value
    // stored under the primary name (--spec-draft-model) would both be emitted.
    private static void MarkEmitted(HashSet<string> emitted, string flagKey)
    {
        emitted.Add(flagKey);
        var schemaFlag = LlamaServerFlagSchema.FindByName(flagKey);
        if (schemaFlag is null) return;
        foreach (var name in schemaFlag.Names)
            emitted.Add(name);
    }

    private static bool EmitFlag(string flagKey, IReadOnlyDictionary<string, string> flagValues, IReadOnlySet<string>? supportedFlags, List<string> tokens, bool explicitValue, ICollection<string>? droppedFlags = null)
    {
        if (!flagValues.TryGetValue(flagKey, out var value) || string.IsNullOrWhiteSpace(value))
            return false;

        var schemaFlag = LlamaServerFlagSchema.FindByName(flagKey);
        if (schemaFlag is null)
        {
            // Generic flags are emitted as-is; we have no schema for default/runtime checks.
            tokens.Add(flagKey);
            tokens.Add(value);
            return true;
        }

        if (string.Equals(schemaFlag.PrimaryName, "--model", StringComparison.OrdinalIgnoreCase))
            return false;
        if (schemaFlag.IsSecurityCritical)
            return false;

        if (!IsSupportedByRuntime(schemaFlag, flagKey, supportedFlags))
        {
            droppedFlags?.Add(flagKey);
            return false;
        }

        if (schemaFlag.ValueType == FlagValueType.Boolean)
        {
            if (IsTruthyBoolean(value))
            {
                if (explicitValue || !IsDefaultFlagValue(schemaFlag, value))
                {
                    tokens.Add(flagKey);
                    // Tri-state flags (default "auto", e.g. --flash-attn) require an explicit
                    // value token, mirroring the "off" pair below; a bare flag would make the
                    // server consume the next argument as this flag's value.
                    if (string.Equals(
                        Convert.ToString(schemaFlag.Default, CultureInfo.InvariantCulture),
                        "auto",
                        StringComparison.OrdinalIgnoreCase))
                        tokens.Add("on");
                    return true;
                }
            }
            else if (IsFalsyBoolean(value))
            {
                if (explicitValue || !IsDefaultFlagValue(schemaFlag, value))
                {
                    var negatedToken = schemaFlag.NegatedForm is null
                        ? null
                        : schemaFlag.PrimaryName.StartsWith("--no-", StringComparison.OrdinalIgnoreCase)
                            ? schemaFlag.PrimaryName
                            : schemaFlag.NegatedForm;
                    if (!string.IsNullOrWhiteSpace(negatedToken)
                        && IsSupportedByRuntime(schemaFlag, negatedToken, supportedFlags))
                    {
                        tokens.Add(negatedToken);
                        return true;
                    }

                    if (string.Equals(
                        Convert.ToString(schemaFlag.Default, CultureInfo.InvariantCulture),
                        "auto",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        tokens.Add(flagKey);
                        tokens.Add("off");
                        return true;
                    }

                    // The runtime advertises no token that can express "off" for this flag;
                    // record the drop so the launcher can surface it instead of the user's
                    // explicit selection vanishing silently.
                    droppedFlags?.Add(negatedToken ?? flagKey);
                }
            }
            else if (string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase))
            {
                // omit; default value
            }
        }
        else if (schemaFlag.ValueType == FlagValueType.MultiToken)
        {
            var arity = schemaFlag.Arity;
            if (arity <= 1)
            {
                tokens.Add(flagKey);
                tokens.Add(value);
                return true;
            }

            var parts = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != arity)
                throw new InvalidOperationException(
                    $"Flag '{schemaFlag.PrimaryName}' requires exactly {arity} values; got {parts.Length}.");

            tokens.Add(flagKey);
            for (var i = 0; i < arity; i++)
                tokens.Add(parts[i]);
            return true;
        }
        else
        {
            if (explicitValue || !IsDefaultFlagValue(schemaFlag, value))
            {
                tokens.Add(flagKey);
                tokens.Add(value);
                return true;
            }
        }

        return false;
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
        var seen = new Dictionary<string, (string KeyName, string RawName)>(StringComparer.OrdinalIgnoreCase);
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

            var flagName = GetFlagName(token);
            var (schemaFlag, keyName) = ResolveFlag(flagName);
            var (flagValue, valueProvided, consumed) = ConsumeFlagValue(tokens, i, schemaFlag);

            if (consumed == 0)
            {
                extraArgs.Add(token);
                i++;
                continue;
            }

            if (schemaFlag is null || (supportedFlags is not null && !IsSupportedByRuntime(schemaFlag, flagName, supportedFlags)))
            {
                extraArgs.Add(token);
                if (valueProvided && consumed > 1)
                {
                    for (var j = i + 1; j < i + consumed; j++) extraArgs.Add(tokens[j]);
                }
                i += consumed;
                continue;
            }

            if (schemaFlag.ValueType == FlagValueType.MultiToken
                && valueProvided
                && i + consumed < tokens.Count
                && !IsFlag(tokens[i + consumed]))
            {
                var supplied = flagValue?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
                while (i + consumed < tokens.Count && !IsFlag(tokens[i + consumed]))
                {
                    supplied++;
                    consumed++;
                }

                errors.Add($"Flag '{schemaFlag.PrimaryName}' requires exactly {schemaFlag.Arity} values; got {supplied}.");
                i += consumed;
                continue;
            }

            if (schemaFlag.ValueType == FlagValueType.Boolean && valueProvided && flagValue is not null && !IsBooleanString(flagValue))
            {
                consumed = 1;
                valueProvided = false;
                flagValue = null;
            }

            if (seen.TryGetValue(schemaFlag.PrimaryName, out var oldEntry))
            {
                if (schemaFlag.ValueType == FlagValueType.Boolean
                    && IsNegatedBooleanToken(oldEntry.RawName, schemaFlag) != IsNegatedBooleanToken(flagName, schemaFlag))
                {
                    errors.Add($"Conflicting flags '{oldEntry.RawName}' and '{flagName}' cannot be used together.");
                    i += consumed;
                    continue;
                }
                flags.Remove(oldEntry.KeyName);
            }
            seen[schemaFlag.PrimaryName] = (keyName, flagName);

            if (schemaFlag.ValueType == FlagValueType.Boolean)
            {
                var isNegation = IsNegatedBooleanToken(flagName, schemaFlag);
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
            else if (schemaFlag.ValueType == FlagValueType.MultiToken)
            {
                if (!valueProvided)
                {
                    errors.Add($"Flag '{schemaFlag.PrimaryName}' requires {schemaFlag.Arity} values.");
                }
                else
                {
                    var parts = flagValue!.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != schemaFlag.Arity)
                    {
                        errors.Add($"Flag '{schemaFlag.PrimaryName}' requires exactly {schemaFlag.Arity} values; got {parts.Length}.");
                    }
                    else
                    {
                        flags[keyName] = flagValue;
                    }
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

        foreach (var disallowed in SecurityCriticalFlagNames())
        {
            if (string.Equals(token, disallowed, StringComparison.OrdinalIgnoreCase)
                || token.StartsWith(disallowed + "=", StringComparison.OrdinalIgnoreCase)
                || token.StartsWith(disallowed + " ", StringComparison.OrdinalIgnoreCase))
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

    private static string GetFlagName(string token)
    {
        var equals = token.IndexOf('=', StringComparison.Ordinal);
        return equals > 0 ? token[..equals] : token;
    }

    private static (string? Value, bool ValueProvided, int Consumed) ConsumeFlagValue(IReadOnlyList<string> tokens, int i, LlamaServerFlag? schemaFlag)
    {
        var token = tokens[i];
        var equals = token.IndexOf('=', StringComparison.Ordinal);
        if (equals > 0)
        {
            var valuePart = token[(equals + 1)..];
            if (string.IsNullOrWhiteSpace(valuePart))
                return (null, false, 1);

            if (schemaFlag?.ValueType == FlagValueType.MultiToken && schemaFlag.Arity > 1)
            {
                var parts = valuePart.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
                var consumed = 1;
                while (parts.Count < schemaFlag.Arity && i + consumed < tokens.Count && !IsFlag(tokens[i + consumed]))
                {
                    parts.Add(tokens[i + consumed]);
                    consumed++;
                }
                return (string.Join(" ", parts), true, consumed);
            }

            return (valuePart, true, 1);
        }

        if (i + 1 >= tokens.Count || IsFlag(tokens[i + 1]))
            return (null, false, 1);

        if (schemaFlag?.ValueType == FlagValueType.MultiToken && schemaFlag.Arity > 1)
        {
            var parts = new List<string>();
            var consumed = 1;
            while (parts.Count < schemaFlag.Arity && i + consumed < tokens.Count && !IsFlag(tokens[i + consumed]))
            {
                parts.Add(tokens[i + consumed]);
                consumed++;
            }
            return (string.Join(" ", parts), parts.Count > 0, consumed);
        }

        if (schemaFlag?.ValueType == FlagValueType.Boolean)
        {
            var next = tokens[i + 1];
            if (IsBooleanString(next))
                return (next, true, 2);
            return (null, false, 1);
        }

        return (tokens[i + 1], true, 2);
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
        if (flag?.ValueType == FlagValueType.Boolean
            && flag.PrimaryName.StartsWith("--no-", StringComparison.OrdinalIgnoreCase)
            && flag.NegatedForm is not null)
        {
            var positiveFlag = LlamaServerFlagSchema.FindByName(flag.NegatedForm);
            if (positiveFlag?.ValueType == FlagValueType.Boolean)
                return (positiveFlag, positiveFlag.PrimaryName);
        }

        var keyName = flag is null
            ? token
            : flag.Names.FirstOrDefault(n => n.Equals(token, StringComparison.OrdinalIgnoreCase)) ?? flag.PrimaryName;
        return (flag, keyName);
    }

    private static bool IsNegatedBooleanToken(string token, LlamaServerFlag positiveFlag)
    {
        var tokenFlag = LlamaServerFlagSchema.FindByName(token);
        if (tokenFlag?.ValueType == FlagValueType.Boolean
            && tokenFlag.PrimaryName.StartsWith("--no-", StringComparison.OrdinalIgnoreCase)
            && string.Equals(tokenFlag.NegatedForm, positiveFlag.PrimaryName, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!token.StartsWith("--no-", StringComparison.OrdinalIgnoreCase))
            return false;

        var positiveName = "--" + token[5..];
        var resolvedPositive = LlamaServerFlagSchema.FindByName(positiveName);
        return resolvedPositive?.ValueType == FlagValueType.Boolean
            && string.Equals(resolvedPositive.PrimaryName, positiveFlag.PrimaryName, StringComparison.OrdinalIgnoreCase);
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

    public static bool IsDefaultFlagValue(LlamaServerFlag? flag, string? value)
    {
        if (flag is null) return true;
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (flag.ValueType == FlagValueType.Boolean && string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase))
            return true;
        if (flag.Default is null)
        {
            if (flag.ValueType == FlagValueType.Boolean)
                return !IsTruthyBoolean(value);
            return false;
        }
        return IsDefaultValue(flag, value);
    }

    /// <summary>Removes empty, auto/unset, and non-pair false boolean flag values, preserving unknown flags and explicit values (including those matching schema defaults).</summary>
    public static IReadOnlyDictionary<string, string> SanitizeFlagValues(IReadOnlyDictionary<string, string> flagValues)
    {
        if (flagValues.Count == 0) return flagValues;

        var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in CanonicalizeFlagValues(flagValues))
        {
            if (string.IsNullOrWhiteSpace(kvp.Value)) continue;
            var flag = LlamaServerFlagSchema.FindByName(kvp.Key);
            if (flag is not null && flag.ValueType == FlagValueType.Boolean)
            {
                if (string.Equals(kvp.Value, "auto", StringComparison.OrdinalIgnoreCase)) continue;
                if (IsFalsyBoolean(kvp.Value) && flag.NegatedForm is null) continue;
            }
            builder[kvp.Key] = kvp.Value;
        }

        return builder.ToImmutable();
    }

    internal static IReadOnlyDictionary<string, string> CanonicalizeFlagValues(IReadOnlyDictionary<string, string> flagValues)
    {
        if (flagValues.Count == 0) return flagValues;

        var canonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Current positive/canonical entries win over legacy --no-* entries if both exist.
        foreach (var (key, value) in flagValues)
        {
            var flag = LlamaServerFlagSchema.FindByName(key);
            if (flag?.ValueType == FlagValueType.Boolean
                && flag.PrimaryName.StartsWith("--no-", StringComparison.OrdinalIgnoreCase)
                && flag.NegatedForm is not null)
                continue;

            canonical[flag?.PrimaryName ?? key] = value;
        }

        foreach (var (key, value) in flagValues)
        {
            var flag = LlamaServerFlagSchema.FindByName(key);
            if (flag?.ValueType != FlagValueType.Boolean
                || !flag.PrimaryName.StartsWith("--no-", StringComparison.OrdinalIgnoreCase)
                || flag.NegatedForm is null)
                continue;

            var positiveFlag = LlamaServerFlagSchema.FindByName(flag.NegatedForm);
            if (positiveFlag?.ValueType != FlagValueType.Boolean
                || canonical.ContainsKey(positiveFlag.PrimaryName))
                continue;

            if (IsTruthyBoolean(value))
                canonical[positiveFlag.PrimaryName] = "false";
            else if (IsFalsyBoolean(value))
                canonical[positiveFlag.PrimaryName] = "true";
        }

        return canonical;
    }

    // The accepted boolean vocabulary is the contract between the emitter, the parser, the
    // validator, and the UI binder; keep it defined once so a spelling added here cannot pass
    // validation but fail emission (or vice versa).
    internal static bool IsTruthyBoolean(string value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);

    internal static bool IsFalsyBoolean(string value)
        => string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "off", StringComparison.OrdinalIgnoreCase);

    internal static bool IsBooleanString(string value)
        => IsTruthyBoolean(value) || IsFalsyBoolean(value) || string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase);

    private static bool IsExplicitFirstClassValue(
        LlamaServerFlag schemaFlag,
        string flagKey,
        IReadOnlyDictionary<string, string> firstClassValues,
        LlamaServerLaunchOptions options)
    {
        // First-class Boolean fields use "auto" as the unset state. BuildFlagValues only
        // contains them for explicit on/off selections, so matching a runtime default must
        // not cause the selected override to disappear.
        if (schemaFlag.ValueType == FlagValueType.Boolean)
        {
            if (!firstClassValues.TryGetValue(flagKey, out var value))
                return false;
            if (IsTruthyBoolean(value))
                return true;
            if (!IsFalsyBoolean(value))
                return false;
            return schemaFlag.NegatedForm is not null
                || string.Equals(
                    Convert.ToString(schemaFlag.Default, CultureInfo.InvariantCulture),
                    "auto",
                    StringComparison.OrdinalIgnoreCase);
        }

        // 0 is a meaningful value for these flags ("load context size from the model" /
        // "force CPU-only"), not a request for the server's own default, so matching the
        // schema default of 0 must not suppress the flag. Emit them unconditionally.
        if (string.Equals(schemaFlag.PrimaryName, "--ctx-size", StringComparison.OrdinalIgnoreCase)
            || string.Equals(schemaFlag.PrimaryName, "--gpu-layers", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(flagKey, "--cache-ram", StringComparison.OrdinalIgnoreCase))
            return !string.Equals(options.PromptCacheMode, "auto", StringComparison.OrdinalIgnoreCase);

        if (string.Equals(flagKey, "--ctx-checkpoints", StringComparison.OrdinalIgnoreCase)
            || string.Equals(flagKey, "--checkpoint-min-step", StringComparison.OrdinalIgnoreCase))
            return !string.Equals(options.ContextCheckpointsMode, "auto", StringComparison.OrdinalIgnoreCase);

        return false;
    }

    internal static bool IsSupportedByRuntime(LlamaServerFlag schemaFlag, string token, IReadOnlySet<string>? supportedFlags)
    {
        if (supportedFlags is null || supportedFlags.Count == 0) return true;
        if (schemaFlag.Names.Any(n => EssentialFlags.Contains(n, StringComparer.OrdinalIgnoreCase))) return true;
        if (RuntimeSupportsToken(token, supportedFlags)) return true;
        // For negated tokens (--no-*), don't fall back to the positive form.
        if (token.StartsWith("--no-", StringComparison.OrdinalIgnoreCase))
            return false;
        if (schemaFlag.Names.Any(n => RuntimeSupportsToken(n, supportedFlags))) return true;
        return false;
    }

    /// <summary>
    /// Removes schema-known flags (and their value tokens) that the selected runtime does not
    /// advertise from a custom-parameter token list, matching the filtering applied to
    /// first-class flags so a flag the app judged unsupported is not handed to the server
    /// anyway. Schema-unknown tokens are preserved untouched: they may be valid for a build
    /// newer than the schema. Dropped flags are reported via <paramref name="droppedFlags"/>.
    /// </summary>
    internal static IReadOnlyList<string> FilterExtraArgsForRuntime(
        IReadOnlyList<string> extraArgs,
        IReadOnlySet<string>? supportedFlags,
        ICollection<string>? droppedFlags = null)
    {
        if (supportedFlags is null || supportedFlags.Count == 0 || extraArgs.Count == 0)
            return extraArgs;

        var kept = new List<string>(extraArgs.Count);
        var i = 0;
        while (i < extraArgs.Count)
        {
            var token = extraArgs[i];
            if (!IsFlag(token))
            {
                kept.Add(token);
                i++;
                continue;
            }

            var head = GetFlagName(token);
            var schemaFlag = LlamaServerFlagSchema.FindByName(head);
            if (schemaFlag is null && head.StartsWith("--no-", StringComparison.OrdinalIgnoreCase))
                schemaFlag = LlamaServerFlagSchema.FindByName("--" + head[5..]);
            if (schemaFlag is null || IsSupportedByRuntime(schemaFlag, head, supportedFlags))
            {
                kept.Add(token);
                i++;
                continue;
            }

            droppedFlags?.Add(head);
            var (_, _, consumed) = ConsumeFlagValue(extraArgs, i, schemaFlag);
            i += consumed;
        }

        return kept;
    }

    internal static bool RuntimeSupportsToken(string token, IReadOnlySet<string> supportedFlags)
    {
        if (token.StartsWith("--", StringComparison.Ordinal))
        {
            return supportedFlags.Any(n => n.StartsWith("--", StringComparison.Ordinal)
                && string.Equals(n, token, StringComparison.OrdinalIgnoreCase));
        }
        return supportedFlags.Contains(token);
    }

    /// <summary>
    /// Quotes a token so it round-trips through <see cref="CustomLaunchParameterParser"/>.
    /// Shared with the form binder, which re-quotes parsed extra args back into the custom
    /// parameters box: both sides must escape identically or values corrupt on that path.
    /// </summary>
    internal static string QuoteIfNeeded(string value)
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
