namespace LocalLlmConsole.Services;

public sealed record RuntimeMetricDisplaySnapshot(
    string RuntimeKey,
    IReadOnlyList<PrometheusSample> Samples,
    string Tokens,
    string GenerationRate,
    string TotalTokens,
    string MtpTokens,
    string Slots,
    string Settings,
    DateTimeOffset CapturedAt,
    double? GeneratedTokens,
    double? PromptTokens,
    double? MtpGeneratedTokens,
    double? MtpAcceptedTokens,
    double? AverageGenerationRate,
    double? AveragePromptRate,
    double? AverageMtpGeneratedRate,
    double? AverageMtpAcceptedRate,
    DateTimeOffset? GeneratedTokensCapturedAt,
    DateTimeOffset? PromptTokensCapturedAt,
    DateTimeOffset? MtpGeneratedTokensCapturedAt,
    DateTimeOffset? MtpAcceptedTokensCapturedAt,
    DateTimeOffset? AverageGenerationRateCapturedAt,
    DateTimeOffset? AveragePromptRateCapturedAt,
    DateTimeOffset? AverageMtpGeneratedRateCapturedAt,
    DateTimeOffset? AverageMtpAcceptedRateCapturedAt);

public sealed record RuntimeMetricSummaryResult(
    string Tokens,
    string GenerationRate,
    string TotalTokens,
    string MtpTokens,
    string Slots,
    string Settings,
    bool UsedLastKnown,
    DateTimeOffset? LastKnownCapturedAt);

public sealed class RuntimeMetricSummaryTracker
{
    private readonly Dictionary<string, RuntimeMetricSummaryState> _states = new(StringComparer.Ordinal);

    public RuntimeMetricSummaryResult Apply(
        string runtimeKey,
        IReadOnlyList<PrometheusSample> samples,
        AppSettings metricsSettings,
        RuntimeSlotSnapshot? slotSnapshot,
        RuntimeMtpTokenSnapshot? mtpTokenSnapshot,
        DateTimeOffset? capturedAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimeKey);
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(metricsSettings);

        var state = StateFor(runtimeKey);
        var previous = state.LastDisplay;

        if (samples.Count == 0
            && slotSnapshot is null
            && mtpTokenSnapshot is null
            && previous is { } snapshot)
        {
            return new RuntimeMetricSummaryResult(
                snapshot.Tokens,
                snapshot.GenerationRate,
                snapshot.TotalTokens,
                snapshot.MtpTokens,
                snapshot.Slots,
                snapshot.Settings,
                UsedLastKnown: true,
                LastKnownCapturedAt(snapshot));
        }

        var now = capturedAt ?? DateTimeOffset.UtcNow;
        var predictedTokens = RuntimeDashboardService.GeneratedTokenCounter(samples);
        var predictedSeconds = RuntimeMetrics.Sum(samples, ["tokens", "predicted", "seconds", "total"], [])
            ?? RuntimeMetrics.Sum(samples, ["tokens", "generated", "seconds", "total"], [])
            ?? RuntimeMetrics.Sum(samples, ["eval", "time"], ["prompt"]);
        var promptTokens = RuntimeDashboardService.PromptTokenCounter(samples);
        var promptSeconds = RuntimeMetrics.Sum(samples, ["prompt", "seconds", "total"], [])
            ?? RuntimeMetrics.Sum(samples, ["prompt", "time"], []);
        var observedMtpGeneratedTokens = RuntimeDashboardService.MaxNullable(
            RuntimeDashboardService.MaxNullable(RuntimeDashboardService.MtpGeneratedTokenCounter(samples), slotSnapshot?.MtpGeneratedTokens),
            mtpTokenSnapshot?.GeneratedTokens);
        var observedMtpAcceptedTokens = RuntimeDashboardService.MaxNullable(
            RuntimeDashboardService.MaxNullable(RuntimeDashboardService.MtpAcceptedTokenCounter(samples), slotSnapshot?.MtpAcceptedTokens),
            mtpTokenSnapshot?.AcceptedTokens);
        var mtpGeneratedSeconds = RuntimeDashboardService.MtpGeneratedSecondsCounter(samples)
            ?? mtpTokenSnapshot?.GeneratedSeconds;
        var mtpAcceptedSeconds = RuntimeDashboardService.MtpAcceptedSecondsCounter(samples)
            ?? mtpTokenSnapshot?.AcceptedSeconds
            ?? mtpGeneratedSeconds;

        var liveGenerationRate = CounterRateAndRemember(predictedTokens, ref state.LastPredictedTokenCounter, ref state.LastPredictedTokenPollAt, now);
        var livePromptRate = CounterRateAndRemember(promptTokens, ref state.LastPromptTokenCounter, ref state.LastPromptTokenPollAt, now);
        var liveMtpGeneratedRate = CounterRateAndRemember(observedMtpGeneratedTokens, ref state.LastMtpGeneratedTokenCounter, ref state.LastMtpGeneratedTokenPollAt, now);
        var liveMtpAcceptedRate = CounterRateAndRemember(observedMtpAcceptedTokens, ref state.LastMtpAcceptedTokenCounter, ref state.LastMtpAcceptedTokenPollAt, now);

        var (slotPromptRate, slotGenerationRate) = SlotLiveRates(state, slotSnapshot, now);
        liveGenerationRate = slotGenerationRate ?? liveGenerationRate;
        livePromptRate = slotPromptRate ?? livePromptRate;

        var observedAverageGenerationRate = RuntimeMetrics.First(samples, ["predicted", "tokens", "seconds"], ["total"])
            ?? RuntimeMetrics.First(samples, ["generation", "tokens", "seconds"], ["total"])
            ?? RuntimeDashboardService.Rate(predictedTokens, predictedSeconds);
        var observedAveragePromptRate = RuntimeMetrics.First(samples, ["prompt", "tokens", "seconds"], ["total"])
            ?? RuntimeDashboardService.Rate(promptTokens, promptSeconds);
        var observedAverageMtpGeneratedRate = RuntimeDashboardService.Rate(observedMtpGeneratedTokens, mtpGeneratedSeconds);
        var observedAverageMtpAcceptedRate = RuntimeDashboardService.Rate(observedMtpAcceptedTokens, mtpAcceptedSeconds);
        var displayAverageGenerationRate = observedAverageGenerationRate ?? previous?.AverageGenerationRate;
        var displayAveragePromptRate = observedAveragePromptRate ?? previous?.AveragePromptRate;
        var displayAverageMtpGeneratedRate = observedAverageMtpGeneratedRate ?? previous?.AverageMtpGeneratedRate;
        var displayAverageMtpAcceptedRate = observedAverageMtpAcceptedRate ?? previous?.AverageMtpAcceptedRate;
        var kvUsage = RuntimeMetrics.First(samples, ["kv", "cache", "usage"], []);
        var kvTokens = RuntimeMetrics.Sum(samples, ["kv", "cache", "tokens"], [])
            ?? RuntimeMetrics.Sum(samples, ["kv", "tokens"], []);
        var contextSize = RuntimeMetrics.First(samples, ["context", "size"], [])
            ?? RuntimeMetrics.First(samples, ["ctx", "size"], [])
            ?? slotSnapshot?.ContextSize
            ?? (metricsSettings.ContextSize > 0 ? (double?)metricsSettings.ContextSize : null);
        kvTokens ??= slotSnapshot?.ContextTokens;

        var observedGeneratedTokens = RuntimeDashboardService.MaxNullable(predictedTokens, slotSnapshot?.GeneratedTokens);
        var observedPromptTokens = RuntimeDashboardService.MaxNullable(promptTokens, slotSnapshot?.PromptTokensProcessed);
        var displayGeneratedTokens = RuntimeDashboardService.MaxNullable(observedGeneratedTokens, previous?.GeneratedTokens);
        var displayPromptTokens = RuntimeDashboardService.MaxNullable(observedPromptTokens, previous?.PromptTokens);
        var displayMtpGeneratedTokens = RuntimeDashboardService.MaxNullable(observedMtpGeneratedTokens, previous?.MtpGeneratedTokens);
        var displayMtpAcceptedTokens = RuntimeDashboardService.MaxNullable(observedMtpAcceptedTokens, previous?.MtpAcceptedTokens);
        var usedPreviousGeneratedTokens = UsedPreviousCounter(observedGeneratedTokens, previous?.GeneratedTokens, displayGeneratedTokens);
        var usedPreviousPromptTokens = UsedPreviousCounter(observedPromptTokens, previous?.PromptTokens, displayPromptTokens);
        var usedPreviousMtpGeneratedTokens = UsedPreviousCounter(observedMtpGeneratedTokens, previous?.MtpGeneratedTokens, displayMtpGeneratedTokens);
        var usedPreviousMtpAcceptedTokens = UsedPreviousCounter(observedMtpAcceptedTokens, previous?.MtpAcceptedTokens, displayMtpAcceptedTokens);
        var usedPreviousAverageGenerationRate = UsedPreviousAverage(observedAverageGenerationRate, previous?.AverageGenerationRate);
        var usedPreviousAveragePromptRate = UsedPreviousAverage(observedAveragePromptRate, previous?.AveragePromptRate);
        var usedPreviousAverageMtpGeneratedRate = UsedPreviousAverage(observedAverageMtpGeneratedRate, previous?.AverageMtpGeneratedRate);
        var usedPreviousAverageMtpAcceptedRate = UsedPreviousAverage(observedAverageMtpAcceptedRate, previous?.AverageMtpAcceptedRate);
        var usedLastKnown = usedPreviousGeneratedTokens
            || usedPreviousPromptTokens
            || usedPreviousMtpGeneratedTokens
            || usedPreviousMtpAcceptedTokens
            || usedPreviousAverageGenerationRate
            || usedPreviousAveragePromptRate
            || usedPreviousAverageMtpGeneratedRate
            || usedPreviousAverageMtpAcceptedRate;
        var generatedTokensCapturedAt = DisplayValueCapturedAt(observedGeneratedTokens, displayGeneratedTokens, previous?.GeneratedTokensCapturedAt ?? previous?.CapturedAt, now);
        var promptTokensCapturedAt = DisplayValueCapturedAt(observedPromptTokens, displayPromptTokens, previous?.PromptTokensCapturedAt ?? previous?.CapturedAt, now);
        var mtpGeneratedTokensCapturedAt = DisplayValueCapturedAt(observedMtpGeneratedTokens, displayMtpGeneratedTokens, previous?.MtpGeneratedTokensCapturedAt ?? previous?.CapturedAt, now);
        var mtpAcceptedTokensCapturedAt = DisplayValueCapturedAt(observedMtpAcceptedTokens, displayMtpAcceptedTokens, previous?.MtpAcceptedTokensCapturedAt ?? previous?.CapturedAt, now);
        var averageGenerationRateCapturedAt = DisplayValueCapturedAt(observedAverageGenerationRate, displayAverageGenerationRate, previous?.AverageGenerationRateCapturedAt ?? previous?.CapturedAt, now);
        var averagePromptRateCapturedAt = DisplayValueCapturedAt(observedAveragePromptRate, displayAveragePromptRate, previous?.AveragePromptRateCapturedAt ?? previous?.CapturedAt, now);
        var averageMtpGeneratedRateCapturedAt = DisplayValueCapturedAt(observedAverageMtpGeneratedRate, displayAverageMtpGeneratedRate, previous?.AverageMtpGeneratedRateCapturedAt ?? previous?.CapturedAt, now);
        var averageMtpAcceptedRateCapturedAt = DisplayValueCapturedAt(observedAverageMtpAcceptedRate, displayAverageMtpAcceptedRate, previous?.AverageMtpAcceptedRateCapturedAt ?? previous?.CapturedAt, now);
        var lastKnownCapturedAt = OldestCapturedAt(
            usedPreviousGeneratedTokens ? generatedTokensCapturedAt : null,
            usedPreviousPromptTokens ? promptTokensCapturedAt : null,
            usedPreviousMtpGeneratedTokens ? mtpGeneratedTokensCapturedAt : null,
            usedPreviousMtpAcceptedTokens ? mtpAcceptedTokensCapturedAt : null,
            usedPreviousAverageGenerationRate ? averageGenerationRateCapturedAt : null,
            usedPreviousAveragePromptRate ? averagePromptRateCapturedAt : null,
            usedPreviousAverageMtpGeneratedRate ? averageMtpGeneratedRateCapturedAt : null,
            usedPreviousAverageMtpAcceptedRate ? averageMtpAcceptedRateCapturedAt : null);

        var generationRateText = $"Gen {RuntimeDashboardService.RateLabel(liveGenerationRate, displayAverageGenerationRate)}\nPrompt {RuntimeDashboardService.RateLabel(livePromptRate, displayAveragePromptRate)}";
        var totalTokensText = RuntimeDashboardService.TokenSummaryLabel(displayGeneratedTokens, displayPromptTokens);
        var tokensText = RuntimeDashboardService.TokenActivitySummaryLabel(
            liveGenerationRate,
            displayAverageGenerationRate,
            livePromptRate,
            displayAveragePromptRate,
            displayGeneratedTokens,
            displayPromptTokens);
        var mtpTokensText = MtpTokensText(
            metricsSettings,
            liveMtpGeneratedRate,
            displayAverageMtpGeneratedRate,
            liveMtpAcceptedRate,
            displayAverageMtpAcceptedRate,
            displayMtpGeneratedTokens,
            displayMtpAcceptedTokens);
        var slotsText = RuntimeDashboardService.RuntimeSlotsLabel(samples);
        var settingsText = RuntimeDashboardService.RuntimeSettingsLabel(
            kvUsage,
            kvTokens,
            contextSize,
            metricsSettings.ContextSize,
            metricsSettings.ParallelSlots,
            metricsSettings.KvUnified);
        var snapshotCapturedAt = usedLastKnown && previous is not null ? previous.CapturedAt : now;

        Remember(
            state,
            runtimeKey,
            samples,
            tokensText,
            generationRateText,
            totalTokensText,
            mtpTokensText,
            slotsText,
            settingsText,
            displayGeneratedTokens,
            displayPromptTokens,
            displayMtpGeneratedTokens,
            displayMtpAcceptedTokens,
            displayAverageGenerationRate,
            displayAveragePromptRate,
            displayAverageMtpGeneratedRate,
            displayAverageMtpAcceptedRate,
            generatedTokensCapturedAt,
            promptTokensCapturedAt,
            mtpGeneratedTokensCapturedAt,
            mtpAcceptedTokensCapturedAt,
            averageGenerationRateCapturedAt,
            averagePromptRateCapturedAt,
            averageMtpGeneratedRateCapturedAt,
            averageMtpAcceptedRateCapturedAt,
            snapshotCapturedAt);
        return new RuntimeMetricSummaryResult(
            tokensText,
            generationRateText,
            totalTokensText,
            mtpTokensText,
            slotsText,
            settingsText,
            usedLastKnown,
            usedLastKnown ? lastKnownCapturedAt : null);
    }

    public IReadOnlyList<PrometheusSample> LastKnownSamples(string runtimeKey)
        => _states.TryGetValue(runtimeKey, out var state)
           && state.LastDisplay is { Samples.Count: > 0 } snapshot
            ? snapshot.Samples
            : [];

    public void Reset()
    {
        _states.Clear();
    }

    private static (double? PromptRate, double? GenerationRate) SlotLiveRates(
        RuntimeMetricSummaryState state,
        RuntimeSlotSnapshot? snapshot,
        DateTimeOffset now)
    {
        if (snapshot is null)
            return (null, null);

        double? promptRate = null;
        double? generationRate = null;
        if (state.LastSlotPollAt is not null)
        {
            var elapsed = (now - state.LastSlotPollAt.Value).TotalSeconds;
            if (elapsed >= 0.25)
            {
                promptRate = RuntimeDashboardService.DeltaRate(snapshot.PromptTokensProcessed, state.LastSlotPromptProcessedCounter, elapsed, includeZero: true);
                generationRate = RuntimeDashboardService.DeltaRate(snapshot.GeneratedTokens, state.LastSlotGeneratedCounter, elapsed, includeZero: true);
            }
        }

        state.LastSlotPromptProcessedCounter = snapshot.PromptTokensProcessed;
        state.LastSlotGeneratedCounter = snapshot.GeneratedTokens;
        state.LastSlotPollAt = now;
        return (promptRate, generationRate);
    }

    private static void Remember(
        RuntimeMetricSummaryState state,
        string runtimeKey,
        IReadOnlyList<PrometheusSample> samples,
        string tokensText,
        string generationRateText,
        string totalTokensText,
        string mtpTokensText,
        string slotsText,
        string settingsText,
        double? displayGeneratedTokens,
        double? displayPromptTokens,
        double? displayMtpGeneratedTokens,
        double? displayMtpAcceptedTokens,
        double? averageGenerationRate,
        double? averagePromptRate,
        double? averageMtpGeneratedRate,
        double? averageMtpAcceptedRate,
        DateTimeOffset? generatedTokensCapturedAt,
        DateTimeOffset? promptTokensCapturedAt,
        DateTimeOffset? mtpGeneratedTokensCapturedAt,
        DateTimeOffset? mtpAcceptedTokensCapturedAt,
        DateTimeOffset? averageGenerationRateCapturedAt,
        DateTimeOffset? averagePromptRateCapturedAt,
        DateTimeOffset? averageMtpGeneratedRateCapturedAt,
        DateTimeOffset? averageMtpAcceptedRateCapturedAt,
        DateTimeOffset capturedAt)
    {
        if (displayGeneratedTokens is null
            && displayPromptTokens is null
            && displayMtpGeneratedTokens is null
            && displayMtpAcceptedTokens is null
            && averageGenerationRate is null
            && averagePromptRate is null
            && averageMtpGeneratedRate is null
            && averageMtpAcceptedRate is null
            && samples.Count == 0)
            return;

        var cachedSamples = samples.Count > 0
            ? samples.ToArray()
            : state.LastDisplay is { } previous
                ? previous.Samples
                : [];

        state.LastDisplay = new RuntimeMetricDisplaySnapshot(
            runtimeKey,
            cachedSamples,
            tokensText,
            generationRateText,
            totalTokensText,
            mtpTokensText,
            slotsText,
            settingsText,
            capturedAt,
            displayGeneratedTokens,
            displayPromptTokens,
            displayMtpGeneratedTokens,
            displayMtpAcceptedTokens,
            averageGenerationRate,
            averagePromptRate,
            averageMtpGeneratedRate,
            averageMtpAcceptedRate,
            generatedTokensCapturedAt,
            promptTokensCapturedAt,
            mtpGeneratedTokensCapturedAt,
            mtpAcceptedTokensCapturedAt,
            averageGenerationRateCapturedAt,
            averagePromptRateCapturedAt,
            averageMtpGeneratedRateCapturedAt,
            averageMtpAcceptedRateCapturedAt);
    }

    private static string MtpTokensText(
        AppSettings metricsSettings,
        double? liveGeneratedRate,
        double? averageGeneratedRate,
        double? liveAcceptedRate,
        double? averageAcceptedRate,
        double? generatedTotal,
        double? acceptedTotal)
    {
        if (generatedTotal is null && acceptedTotal is null && !MtpConfigured(metricsSettings))
            return "Inactive";

        return RuntimeDashboardService.MtpTokenSummaryLabel(
            liveGeneratedRate,
            averageGeneratedRate,
            liveAcceptedRate,
            averageAcceptedRate,
            generatedTotal,
            acceptedTotal);
    }

    private static bool MtpConfigured(AppSettings metricsSettings)
        => LaunchSettingMetadataService.NormalizeSpeculativeType(metricsSettings.SpeculativeType)
            .Contains("mtp", StringComparison.OrdinalIgnoreCase);

    private RuntimeMetricSummaryState StateFor(string runtimeKey)
    {
        if (!_states.TryGetValue(runtimeKey, out var state))
        {
            state = new RuntimeMetricSummaryState();
            _states[runtimeKey] = state;
        }

        return state;
    }

    private static double? CounterRateAndRemember(
        double? current,
        ref double? previous,
        ref DateTimeOffset? previousPollAt,
        DateTimeOffset now)
    {
        var rate = RuntimeDashboardService.CounterRate(current, previous, now, previousPollAt, 0.5);
        if (current is not null)
        {
            previous = current;
            previousPollAt = now;
        }

        return rate;
    }

    private static bool UsedPreviousCounter(double? observed, double? previous, double? display)
        => previous is not null
           && display == previous
           && (observed is null || observed.Value < previous.Value);

    private static bool UsedPreviousAverage(double? observed, double? previous)
        => observed is null && previous is not null;

    private static DateTimeOffset? DisplayValueCapturedAt(
        double? observed,
        double? display,
        DateTimeOffset? previousCapturedAt,
        DateTimeOffset now)
    {
        if (display is null) return null;
        return observed is not null && observed.Value == display.Value ? now : previousCapturedAt;
    }

    private static DateTimeOffset? LastKnownCapturedAt(RuntimeMetricDisplaySnapshot snapshot)
        => OldestCapturedAt(
               snapshot.GeneratedTokensCapturedAt,
               snapshot.PromptTokensCapturedAt,
               snapshot.MtpGeneratedTokensCapturedAt,
               snapshot.MtpAcceptedTokensCapturedAt,
               snapshot.AverageGenerationRateCapturedAt,
               snapshot.AveragePromptRateCapturedAt,
               snapshot.AverageMtpGeneratedRateCapturedAt,
               snapshot.AverageMtpAcceptedRateCapturedAt)
           ?? snapshot.CapturedAt;

    private static DateTimeOffset? OldestCapturedAt(params DateTimeOffset?[] capturedAt)
    {
        DateTimeOffset? oldest = null;
        foreach (var timestamp in capturedAt)
        {
            if (timestamp is null) continue;
            if (oldest is null || timestamp.Value < oldest.Value)
                oldest = timestamp;
        }

        return oldest;
    }

    private sealed class RuntimeMetricSummaryState
    {
        public double? LastPredictedTokenCounter;
        public DateTimeOffset? LastPredictedTokenPollAt;
        public double? LastPromptTokenCounter;
        public DateTimeOffset? LastPromptTokenPollAt;
        public double? LastMtpGeneratedTokenCounter;
        public DateTimeOffset? LastMtpGeneratedTokenPollAt;
        public double? LastMtpAcceptedTokenCounter;
        public DateTimeOffset? LastMtpAcceptedTokenPollAt;
        public double? LastSlotPromptProcessedCounter;
        public double? LastSlotGeneratedCounter;
        public DateTimeOffset? LastSlotPollAt;
        public RuntimeMetricDisplaySnapshot? LastDisplay;
    }
}
