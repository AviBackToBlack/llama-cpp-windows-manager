
namespace LocalLlmConsole.Services;

/// <summary>Provides labels, tooltips, and normalized option values for launch settings UI.</summary>
public static class LaunchSettingMetadataService
{
    public const string AtomicMtpSpeculativeType = "atomic-mtp";

    public static readonly IReadOnlyList<string> AutoOnOffOptions = ["auto", "on", "off"];
    public static readonly IReadOnlyList<string> OnOffOptions = ["on", "off"];
    public static readonly IReadOnlyList<string> OffOnOptions = ["off", "on"];
    public static readonly IReadOnlyList<string> CacheTypeOptions = ["f16", "q8_0", "q4_0", "q4_1", "iq4_nl", "q5_0", "q5_1", "f32", "bf16"];
    public static readonly IReadOnlyList<string> SpeculativeTypeOptions = ["none", AtomicMtpSpeculativeType, "draft-mtp", "draft-simple", "draft-eagle3", "ngram-simple", "ngram-map-k", "ngram-map-k4v", "ngram-mod", "ngram-cache"];
    public static readonly IReadOnlyList<string> ReasoningFormatOptions = ["auto", "none", "deepseek", "deepseek-legacy"];
    public static readonly IReadOnlyList<string> RopeScalingOptions = ["auto", "none", "linear", "yarn"];

    public static string NormalizeSpeculativeType(string value)
    {
        var normalized = (value ?? "")
            .Trim()
            .ToLowerInvariant()
            .Replace('_', '-')
            .Replace(' ', '-');
        return normalized == "mtp" ? AtomicMtpSpeculativeType : normalized;
    }

    public static bool IsAtomicMtpSpeculativeType(string value)
        => NormalizeSpeculativeType(value).Equals(AtomicMtpSpeculativeType, StringComparison.OrdinalIgnoreCase);

    public static string LlamaSpeculativeTypeArgument(string value)
        => IsAtomicMtpSpeculativeType(value) ? "mtp" : NormalizeSpeculativeType(value);

    public static string Tooltip(LlamaServerFlag flag)
        => $"({flag.PrimaryName}) {flag.Description}";

    public static string Tooltip(string label, string flagName)
    {
        var baseTooltip = Tooltip(label);
        var flag = LlamaServerFlagSchema.FindByName(flagName);
        if (flag is not null)
        {
            var defaultTooltip = Loc.T("Tooltip.Field.Default");
            if (string.Equals(baseTooltip, defaultTooltip, StringComparison.Ordinal))
                baseTooltip = flag.Description;
        }

        return $"({flagName}) {baseTooltip}";
    }

    public static string Tooltip(string label) => label switch
    {
        "Context size" => Loc.T("Tooltip.Field.ContextSize"),
        "Parallel slots" => Loc.T("Tooltip.Field.ParallelSlots"),
        "Batch size" => Loc.T("Tooltip.Field.BatchSize"),
        "Micro batch" => Loc.T("Tooltip.Field.MicroBatch"),
        "Threads" => Loc.T("Tooltip.Field.Threads"),
        "GPU layers" => Loc.T("Tooltip.Field.GpuLayers"),
        "Reasoning" => Loc.T("Tooltip.Field.Reasoning"),
        "Reason format" => Loc.T("Tooltip.Field.ReasonFormat"),
        "Reason budget" => Loc.T("Tooltip.Field.ReasonBudget"),
        "Jinja chat" => Loc.T("Tooltip.Field.JinjaChat"),
        "Vision" => Loc.T("Tooltip.Field.Vision"),
        "Vision head" => Loc.T("Tooltip.Field.VisionHead"),
        "Image min" => Loc.T("Tooltip.Field.ImageMin"),
        "Image max" => Loc.T("Tooltip.Field.ImageMax"),
        "Flash attention" => Loc.T("Tooltip.Field.FlashAttention"),
        "K cache" => Loc.T("Tooltip.Field.KCache"),
        "V cache" => Loc.T("Tooltip.Field.VCache"),
        "KV offload" => Loc.T("Tooltip.Field.KvOffload"),
        "Unified KV" => Loc.T("Tooltip.Field.UnifiedKv"),
        "Prompt cache" => Loc.T("Tooltip.Field.PromptCache"),
        "Prompt cache MB" => Loc.T("Tooltip.Field.PromptCacheMb"),
        "Checkpoints" => Loc.T("Tooltip.Field.Checkpoints"),
        "Checkpoint count" => Loc.T("Tooltip.Field.CheckpointCount"),
        "Checkpoint spacing" => Loc.T("Tooltip.Field.CheckpointSpacing"),
        "Continuous batch" => Loc.T("Tooltip.Field.ContinuousBatch"),
        "Memory map" => Loc.T("Tooltip.Field.MemoryMap"),
        "Memory lock" => Loc.T("Tooltip.Field.MemoryLock"),
        "Metrics" => Loc.T("Tooltip.Field.Metrics"),
        "Custom params" => Loc.T("Tooltip.Field.CustomParams"),
        "Temperature" => Loc.T("Tooltip.Field.Temperature"),
        "Top K" => Loc.T("Tooltip.Field.TopK"),
        "Top P" => Loc.T("Tooltip.Field.TopP"),
        "Min P" => Loc.T("Tooltip.Field.MinP"),
        "Max tokens" => Loc.T("Tooltip.Field.MaxTokens"),
        "Seed" => Loc.T("Tooltip.Field.Seed"),
        "Repeat window" => Loc.T("Tooltip.Field.RepeatWindow"),
        "Repeat pen" => Loc.T("Tooltip.Field.RepeatPen"),
        "Presence" => Loc.T("Tooltip.Field.Presence"),
        "Frequency" => Loc.T("Tooltip.Field.Frequency"),
        "RoPE scaling" => Loc.T("Tooltip.Field.RopeScaling"),
        "RoPE scale" => Loc.T("Tooltip.Field.RopeScale"),
        "RoPE base" => Loc.T("Tooltip.Field.RopeBase"),
        "RoPE freq" => Loc.T("Tooltip.Field.RopeFreq"),
        "Spec type" => Loc.T("Tooltip.Field.SpecType"),
        "Draft model" => Loc.T("Tooltip.Field.DraftModel"),
        "MTP head" => Loc.T("Tooltip.Field.MtpHead"),
        "Draft GPU" => Loc.T("Tooltip.Field.DraftGpu"),
        "Draft K cache" => Loc.T("Tooltip.Field.DraftKCache"),
        "Draft V cache" => Loc.T("Tooltip.Field.DraftVCache"),
        "Draft max" => Loc.T("Tooltip.Field.DraftMax"),
        "Draft min" => Loc.T("Tooltip.Field.DraftMin"),
        "Split prob" => Loc.T("Tooltip.Field.SplitProb"),
        "Min prob" => Loc.T("Tooltip.Field.MinProb"),
        "Command line" => Loc.T("Tooltip.Field.CommandLine"),
        _ => TryTooltipFromFlag(label)
    };

    private static string TryTooltipFromFlag(string label)
    {
        var flag = LlamaServerFlagSchema.FindByName(label);
        return flag is not null ? Tooltip(flag) : Loc.T("Tooltip.Field.Default");
    }

    public static string ContextSizeTooltip(string text)
    {
        var tooltip = Tooltip("Context size", "--ctx-size");
        if (!LaunchSettingParser.TryNormalizeContextSize(text, out var value) || value <= 0)
            return tooltip;

        var normalized = value.ToString(CultureInfo.InvariantCulture);
        var compactText = (text ?? "")
            .Replace(",", "", StringComparison.Ordinal)
            .Replace("_", "", StringComparison.Ordinal);
        return string.Equals(compactText, normalized, StringComparison.OrdinalIgnoreCase)
            ? tooltip
            : Loc.T("Tooltip.ContextSizeSuggestion", tooltip, value.ToString("N0", CultureInfo.InvariantCulture));
    }
}
