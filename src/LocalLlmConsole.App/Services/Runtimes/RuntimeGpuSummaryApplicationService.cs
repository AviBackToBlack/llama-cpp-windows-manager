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
        var cpuTask = _gpuStatus.CpuSummaryAsync(cancellationToken);
        if (activeSession?.Backend == RuntimeBackend.Cpu)
            return await cpuTask;

        Task<string> acceleratorTask;
        if (activeSession?.Backend == RuntimeBackend.Cuda)
            acceleratorTask = FirstAvailableAsync(
                [() => _gpuStatus.SummaryAsync(cancellationToken),
                    () => _gpuStatus.WindowsSummaryAsync(cancellationToken)]);
        else if (activeSession?.Backend == RuntimeBackend.Sycl)
            acceleratorTask = FirstAvailableAsync(
                [() => _gpuStatus.WindowsSummaryAsync(cancellationToken),
                    () => activeSession.Mode == RuntimeMode.Wsl
                        ? _gpuStatus.WslIntelArcSummaryAsync(_wslExe(), activeSession.LaunchSettings.WslDistro, cancellationToken)
                        : _gpuStatus.WindowsIntelArcSummaryAsync(cancellationToken)]);
        else
            acceleratorTask = FirstAvailableAsync(
                [() => _gpuStatus.WindowsSummaryAsync(cancellationToken),
                    () => _gpuStatus.SummaryAsync(cancellationToken)]);

        await Task.WhenAll(cpuTask, acceleratorTask);
        return CombinedHardwareSummary(cpuTask.Result, acceleratorTask.Result);
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

    private static string CombinedHardwareSummary(string cpu, string accelerator)
    {
        var lines = new List<string>();
        if (!IsUnavailable(cpu))
            lines.AddRange(cpu.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Take(2));
        if (!IsUnavailable(accelerator))
            lines.AddRange(accelerator.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Take(2));
        return lines.Count == 0 ? "Unavailable" : string.Join(Environment.NewLine, lines);
    }

    private static string CacheKey(LoadedModelSessionSnapshot? activeSession)
        => activeSession is null
            ? "host"
            : $"{activeSession.SessionId}|{activeSession.Mode}|{activeSession.Backend}|{activeSession.LaunchSettings.WslDistro}";
}
