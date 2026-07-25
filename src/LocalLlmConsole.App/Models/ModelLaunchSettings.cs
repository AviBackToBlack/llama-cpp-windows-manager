namespace LocalLlmConsole.Models;

public sealed record ModelLaunchSettings(
    int ContextSize,
    int GpuLayers,
    bool EnableMetrics,
    string ReasoningMode,
    string ReasoningFormat,
    int ReasoningBudget,
    string VisionMode,
    string VisionProjectorPath,
    string FlashAttention,
    string CacheTypeK,
    string CacheTypeV,
    string KvOffload,
    string KvUnified,
    string ContinuousBatching,
    string JinjaMode,
    int ParallelSlots,
    int BatchSize,
    int MicroBatchSize,
    int Threads,
    string MmapMode,
    string MlockMode,
    double Temperature,
    int TopK,
    double TopP,
    double MinP,
    string RuntimeId = "",
    int Port = 0,
    int MaxTokens = AppSettings.DefaultMaxTokens,
    int Seed = AppSettings.DefaultSeed,
    int RepeatLastN = AppSettings.DefaultRepeatLastN,
    double RepeatPenalty = AppSettings.DefaultRepeatPenalty,
    double PresencePenalty = AppSettings.DefaultPresencePenalty,
    double FrequencyPenalty = AppSettings.DefaultFrequencyPenalty,
    string RopeScaling = AppSettings.DefaultRopeScaling,
    double RopeScale = AppSettings.DefaultRopeScale,
    double RopeFreqBase = AppSettings.DefaultRopeFreqBase,
    double RopeFreqScale = AppSettings.DefaultRopeFreqScale,
    string SpeculativeType = AppSettings.DefaultSpeculativeType,
    string SpecDraftModelPath = "",
    string MtpHeadPath = AppSettings.DefaultMtpHeadPath,
    int SpecDraftGpuLayers = AppSettings.DefaultSpecDraftGpuLayers,
    int SpecDraftMinTokens = AppSettings.DefaultSpecDraftMinTokens,
    int SpecDraftMaxTokens = AppSettings.DefaultSpecDraftMaxTokens,
    double SpecDraftPSplit = AppSettings.DefaultSpecDraftPSplit,
    double SpecDraftPMin = AppSettings.DefaultSpecDraftPMin,
    string SpecDraftCacheTypeK = AppSettings.DefaultCacheType,
    string SpecDraftCacheTypeV = AppSettings.DefaultCacheType,
    int VisionImageMinTokens = AppSettings.DefaultVisionImageMinTokens,
    int VisionImageMaxTokens = AppSettings.DefaultVisionImageMaxTokens,
    string PromptCacheMode = AppSettings.DefaultPromptCacheMode,
    int PromptCacheRamMb = AppSettings.DefaultPromptCacheRamMb,
    string ContextCheckpointsMode = AppSettings.DefaultContextCheckpointsMode,
    int ContextCheckpointCount = AppSettings.DefaultContextCheckpointCount,
    int ContextCheckpointEveryNTokens = AppSettings.DefaultContextCheckpointEveryNTokens,
    string CustomParameters = "",
    IReadOnlyDictionary<string, string> FlagValues = null!)
{
    private readonly IReadOnlyDictionary<string, string> _flagValues = FlagValues ?? ImmutableDictionary<string, string>.Empty;
    public IReadOnlyDictionary<string, string> FlagValues
    {
        get => _flagValues;
        init => _flagValues = value ?? ImmutableDictionary<string, string>.Empty;
    }

    private static string Normalize(string? value, IReadOnlyList<string> allowed, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        foreach (var allowedValue in allowed)
        {
            if (string.Equals(allowedValue, value, StringComparison.OrdinalIgnoreCase))
                return allowedValue;
        }

        return defaultValue;
    }

    private ModelLaunchSettings Sanitize()
        => this with
        {
            ReasoningMode = Normalize(ReasoningMode, LaunchSettingMetadataService.AutoOnOffOptions, "auto"),
            ReasoningFormat = Normalize(ReasoningFormat, LaunchSettingMetadataService.ReasoningFormatOptions, "auto"),
            VisionMode = Normalize(VisionMode, LaunchSettingMetadataService.AutoOnOffOptions, "auto"),
            FlashAttention = Normalize(FlashAttention, LaunchSettingMetadataService.AutoOnOffOptions, "auto"),
            CacheTypeK = Normalize(CacheTypeK, LaunchSettingMetadataService.CacheTypeOptions, AppSettings.DefaultCacheType),
            CacheTypeV = Normalize(CacheTypeV, LaunchSettingMetadataService.CacheTypeOptions, AppSettings.DefaultCacheType),
            KvOffload = Normalize(KvOffload, LaunchSettingMetadataService.AutoOnOffOptions, "auto"),
            KvUnified = Normalize(KvUnified, LaunchSettingMetadataService.AutoOnOffOptions, "auto"),
            ContinuousBatching = Normalize(ContinuousBatching, LaunchSettingMetadataService.OnOffOptions, "on"),
            JinjaMode = Normalize(JinjaMode, LaunchSettingMetadataService.AutoOnOffOptions, "auto"),
            MmapMode = Normalize(MmapMode, LaunchSettingMetadataService.AutoOnOffOptions, "auto"),
            MlockMode = Normalize(MlockMode, LaunchSettingMetadataService.OnOffOptions, "off"),
            RopeScaling = Normalize(RopeScaling, LaunchSettingMetadataService.RopeScalingOptions, AppSettings.DefaultRopeScaling),
            SpeculativeType = Normalize(
                LaunchSettingMetadataService.NormalizeSpeculativeType(SpeculativeType),
                LaunchSettingMetadataService.SpeculativeTypeOptions,
                AppSettings.DefaultSpeculativeType),
            SpecDraftCacheTypeK = Normalize(SpecDraftCacheTypeK, LaunchSettingMetadataService.CacheTypeOptions, AppSettings.DefaultCacheType),
            SpecDraftCacheTypeV = Normalize(SpecDraftCacheTypeV, LaunchSettingMetadataService.CacheTypeOptions, AppSettings.DefaultCacheType),
            PromptCacheMode = Normalize(PromptCacheMode, LaunchSettingMetadataService.AutoOnOffOptions, AppSettings.DefaultPromptCacheMode),
            ContextCheckpointsMode = Normalize(ContextCheckpointsMode, LaunchSettingMetadataService.AutoOnOffOptions, AppSettings.DefaultContextCheckpointsMode),
            FlagValues = LaunchCommandService.SanitizeFlagValues(FlagValues)
        };

    public static ModelLaunchSettings FromAppSettings(AppSettings settings, string runtimeId = "")
    {
        return new ModelLaunchSettings(
        settings.ContextSize,
        settings.GpuLayers,
        settings.EnableMetrics,
        settings.ReasoningMode,
        settings.ReasoningFormat,
        settings.ReasoningBudget,
        settings.VisionMode,
        settings.VisionProjectorPath,
        settings.FlashAttention,
        settings.CacheTypeK,
        settings.CacheTypeV,
        settings.KvOffload,
        settings.KvUnified,
        settings.ContinuousBatching,
        settings.JinjaMode,
        settings.ParallelSlots,
        settings.BatchSize,
        settings.MicroBatchSize,
        settings.Threads,
        settings.MmapMode,
        settings.MlockMode,
        settings.Temperature,
        settings.TopK,
        settings.TopP,
        settings.MinP,
        runtimeId,
        settings.Port,
        settings.MaxTokens,
        settings.Seed,
        settings.RepeatLastN,
        settings.RepeatPenalty,
        settings.PresencePenalty,
        settings.FrequencyPenalty,
        settings.RopeScaling,
        settings.RopeScale,
        settings.RopeFreqBase,
        settings.RopeFreqScale,
        settings.SpeculativeType,
        settings.SpecDraftModelPath,
        settings.MtpHeadPath,
        settings.SpecDraftGpuLayers,
        settings.SpecDraftMinTokens,
        settings.SpecDraftMaxTokens,
        settings.SpecDraftPSplit,
        settings.SpecDraftPMin,
        settings.SpecDraftCacheTypeK,
        settings.SpecDraftCacheTypeV,
        settings.VisionImageMinTokens,
        settings.VisionImageMaxTokens,
        settings.PromptCacheMode,
        settings.PromptCacheRamMb,
        settings.ContextCheckpointsMode,
        settings.ContextCheckpointCount,
        settings.ContextCheckpointEveryNTokens,
        settings.CustomParameters,
        settings.FlagValues)
        {
        }.Sanitize();
    }

    public AppSettings ApplyTo(AppSettings settings)
    {
        var s = Sanitize();
        return settings with
        {
            ContextSize = ContextSize,
            GpuLayers = GpuLayers,
            EnableMetrics = EnableMetrics,
            ReasoningMode = s.ReasoningMode,
            ReasoningFormat = s.ReasoningFormat,
            ReasoningBudget = ReasoningBudget,
            VisionMode = s.VisionMode,
            VisionProjectorPath = VisionProjectorPath ?? "",
            FlashAttention = s.FlashAttention,
            CacheTypeK = s.CacheTypeK,
            CacheTypeV = s.CacheTypeV,
            KvOffload = s.KvOffload,
            KvUnified = s.KvUnified,
            ContinuousBatching = s.ContinuousBatching,
            JinjaMode = s.JinjaMode,
            ParallelSlots = ParallelSlots,
            BatchSize = BatchSize,
            MicroBatchSize = MicroBatchSize,
            Threads = Threads,
            MmapMode = s.MmapMode,
            MlockMode = s.MlockMode,
            Temperature = Temperature,
            TopK = TopK,
            TopP = TopP,
            MinP = MinP,
            Port = Port is >= 1 and <= 65535 ? Port : settings.Port,
            MaxTokens = MaxTokens,
            Seed = Seed,
            RepeatLastN = RepeatLastN,
            RepeatPenalty = RepeatPenalty,
            PresencePenalty = PresencePenalty,
            FrequencyPenalty = FrequencyPenalty,
            RopeScaling = s.RopeScaling,
            RopeScale = RopeScale,
            RopeFreqBase = RopeFreqBase,
            RopeFreqScale = RopeFreqScale,
            SpeculativeType = s.SpeculativeType,
            SpecDraftModelPath = SpecDraftModelPath,
            MtpHeadPath = MtpHeadPath ?? "",
            SpecDraftGpuLayers = SpecDraftGpuLayers,
            SpecDraftMinTokens = SpecDraftMinTokens,
            SpecDraftMaxTokens = SpecDraftMaxTokens,
            SpecDraftPSplit = SpecDraftPSplit,
            SpecDraftPMin = SpecDraftPMin,
            SpecDraftCacheTypeK = s.SpecDraftCacheTypeK,
            SpecDraftCacheTypeV = s.SpecDraftCacheTypeV,
            VisionImageMinTokens = VisionImageMinTokens,
            VisionImageMaxTokens = VisionImageMaxTokens,
            PromptCacheMode = s.PromptCacheMode,
            PromptCacheRamMb = PromptCacheRamMb,
            ContextCheckpointsMode = s.ContextCheckpointsMode,
            ContextCheckpointCount = ContextCheckpointCount,
            ContextCheckpointEveryNTokens = ContextCheckpointEveryNTokens,
            CustomParameters = CustomParameters ?? "",
            FlagValues = s.FlagValues
        };
    }
}
