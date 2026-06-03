namespace LocalLlmConsole.Services;

public sealed class RuntimeGpuSummaryApplicationService
{
    private readonly GpuStatusProbeService _gpuStatus;
    private readonly GpuSummaryCache _cache;
    private readonly Func<string> _wslExe;

    public RuntimeGpuSummaryApplicationService(
        GpuStatusProbeService gpuStatus,
        GpuSummaryCache cache,
        Func<string> wslExe)
    {
        _gpuStatus = gpuStatus ?? throw new ArgumentNullException(nameof(gpuStatus));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _wslExe = wslExe ?? throw new ArgumentNullException(nameof(wslExe));
    }

    public async Task<string> SummaryAsync(
        LoadedModelSessionSnapshot? activeSession,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKey(activeSession);
        if (_cache.TryGet(cacheKey, now, out var cachedSummary))
            return cachedSummary;

        var summary = await ProbeSummaryAsync(activeSession, cancellationToken);
        return _cache.Store(cacheKey, summary, now);
    }

    private async Task<string> ProbeSummaryAsync(
        LoadedModelSessionSnapshot? activeSession,
        CancellationToken cancellationToken)
    {
        if (activeSession?.Backend == RuntimeBackend.Cuda)
            return await FirstAvailableAsync(
                [() => _gpuStatus.SummaryAsync(cancellationToken),
                    () => _gpuStatus.WindowsSummaryAsync(cancellationToken)]);

        if (activeSession?.Backend == RuntimeBackend.Cpu)
            return await _gpuStatus.CpuTemperatureAsync(cancellationToken);

        if (activeSession?.Backend == RuntimeBackend.Sycl)
            return await FirstAvailableAsync(
                [() => _gpuStatus.WindowsSummaryAsync(cancellationToken),
                    () => activeSession.Mode == RuntimeMode.Wsl
                        ? _gpuStatus.WslIntelArcSummaryAsync(_wslExe(), activeSession.LaunchSettings.WslDistro, cancellationToken)
                        : _gpuStatus.WindowsIntelArcSummaryAsync(cancellationToken)]);

        return await FirstAvailableAsync(
            [() => _gpuStatus.WindowsSummaryAsync(cancellationToken),
                () => _gpuStatus.SummaryAsync(cancellationToken)]);
    }

    private static async Task<string> FirstAvailableAsync(IReadOnlyList<Func<Task<string>>> probes)
    {
        foreach (var probe in probes)
        {
            var summary = await probe();
            if (!IsUnavailable(summary)) return summary;
        }

        return "Unavailable";
    }

    private static bool IsUnavailable(string summary)
        => string.IsNullOrWhiteSpace(summary)
           || string.Equals(summary.Trim(), "Unavailable", StringComparison.OrdinalIgnoreCase);

    private static string CacheKey(LoadedModelSessionSnapshot? activeSession)
        => activeSession is null
            ? "host"
            : $"{activeSession.SessionId}|{activeSession.Mode}|{activeSession.Backend}|{activeSession.LaunchSettings.WslDistro}";
}
