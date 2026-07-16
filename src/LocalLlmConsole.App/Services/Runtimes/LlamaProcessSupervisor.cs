
namespace LocalLlmConsole.Services;

public enum LlamaRuntimeState
{
    Stopped,
    Loading,
    Loaded,
    Failed
}
public sealed partial class LlamaProcessSupervisor : IDisposable
{
    private readonly WslRuntimeStopService _wslRuntimeStop;
    private readonly NativeRuntimeStopService _nativeRuntimeStop;
    private readonly RuntimeFlagCapabilityService? _runtimeFlagCapabilityService;
    private Process? _process;
    private ProcessJobObjectService? _jobObject;
    private BoundedLogWriter? _log;
    private bool _attached;
    private bool _recovered;
    private RuntimeMode _lastRuntimeMode;
    private int _state = (int)LlamaRuntimeState.Stopped;

    public bool IsRunning => _process is { HasExited: false } || _attached;
    public bool IsRecovered => _recovered;
    public string ActiveModelId { get; private set; } = "";
    public string ActiveRuntimeId { get; private set; } = "";
    public string LogPath { get; private set; } = "";
    public LlamaRuntimeState State
    {
        get => (LlamaRuntimeState)Volatile.Read(ref _state);
        private set => Volatile.Write(ref _state, (int)value);
    }
    public int? LastExitCode { get; private set; }
    public int ProcessId
    {
        get
        {
            try { return _process?.Id ?? 0; }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Could not read llama process id: {ex.Message}");
                return 0;
            }
        }
    }
    public string WslProcessMarker => _lastWslProcessMarker;
    private AppSettings? _lastSettings;
    private string _lastRuntimeExecutablePath = "";
    private string _lastWslProcessMarker = "";
    private string _lastApiKey = "";

    public LlamaProcessSupervisor(
        WslRuntimeStopService wslRuntimeStop,
        NativeRuntimeStopService nativeRuntimeStop,
        RuntimeFlagCapabilityService? runtimeFlagCapabilityService = null)
    {
        _wslRuntimeStop = wslRuntimeStop ?? throw new ArgumentNullException(nameof(wslRuntimeStop));
        _nativeRuntimeStop = nativeRuntimeStop ?? throw new ArgumentNullException(nameof(nativeRuntimeStop));
        _runtimeFlagCapabilityService = runtimeFlagCapabilityService;
    }

    public async Task StartAsync(RuntimeRecord runtime, ModelRecord model, AppSettings settings, string logRoot)
    {
        Stop();
        Directory.CreateDirectory(logRoot);
        LogPath = Path.Combine(logRoot, $"llama-server-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.log");
        _log = new BoundedLogWriter(LogPath, BoundedLogFile.MegabytesToBytes(settings.MaxLogFileSizeMb));
        State = LlamaRuntimeState.Loading;
        LastExitCode = null;
        _attached = false;
        _recovered = false;
        _lastRuntimeMode = runtime.Mode;

        var executable = runtime.Mode == RuntimeMode.Wsl ? ToWslPath(runtime.ExecutablePath) : runtime.ExecutablePath;
        _lastRuntimeExecutablePath = executable;
        _lastWslProcessMarker = runtime.Mode == RuntimeMode.Wsl ? $"local-llm-console-llama-{Guid.NewGuid():N}" : "";
        var modelPath = runtime.Mode == RuntimeMode.Wsl ? ToWslPath(model.ModelPath) : model.ModelPath;
        var embeddedVisionProjector = VisionProjectorSelection.IsEmbeddedOrMainModel(model.ModelPath, settings.VisionProjectorPath);
        if (VisionProjectorSelection.IsExternal(settings.VisionProjectorPath)
            && !embeddedVisionProjector
            && !File.Exists(Path.GetFullPath(settings.VisionProjectorPath.Trim())))
            throw new InvalidOperationException("Configured vision head/projector GGUF file was not found.");
        var mmprojPath = ModelCatalogService.ResolveVisionProjectorPath(model.ModelPath, settings.VisionProjectorPath);
        var visionProjectorPath = string.IsNullOrWhiteSpace(mmprojPath)
            ? null
            : runtime.Mode == RuntimeMode.Wsl ? ToWslPath(mmprojPath) : mmprojPath;
        var draftModelPath = ResolveDraftModelPath(model.ModelPath, settings.SpecDraftModelPath, settings.SpeculativeType);
        var launchDraftModelPath = string.IsNullOrWhiteSpace(draftModelPath)
            ? null
            : runtime.Mode == RuntimeMode.Wsl ? ToWslPath(draftModelPath) : draftModelPath;
        var mtpHeadPath = ResolveMtpHeadPath(model.ModelPath, settings.MtpHeadPath, settings.SpeculativeType);
        if (!string.IsNullOrWhiteSpace(settings.MtpHeadPath) && !File.Exists(Path.GetFullPath(settings.MtpHeadPath.Trim())))
            throw new InvalidOperationException("Configured MTP head GGUF file was not found.");
        var launchMtpHeadPath = string.IsNullOrWhiteSpace(mtpHeadPath)
            ? null
            : runtime.Mode == RuntimeMode.Wsl ? ToWslPath(mtpHeadPath) : mtpHeadPath;
        var allowDirectLanAccess = AppPreferenceService.DirectModelsAllowLanAccess(settings.ModelAccessMode);
        var launchHost = allowDirectLanAccess
            ? string.IsNullOrWhiteSpace(settings.Host) ? "0.0.0.0" : settings.Host
            : "127.0.0.1";
        var extraArgs = new List<string>(CustomLaunchParameterParser.Parse(settings.CustomParameters));

        var request = new RuntimeLaunchRequest
        {
            Mode = runtime.Mode,
            Backend = runtime.Backend,
            ExecutablePath = executable,
            ModelPath = modelPath,
            WslDistro = runtime.Mode == RuntimeMode.Wsl ? settings.WslDistro : "",
            Host = launchHost,
            AllowNetworkAccess = allowDirectLanAccess,
            ApiKey = settings.RequireApiKeyAuth ? settings.ModelApiKey : "",
            RequireApiKeyAuth = settings.RequireApiKeyAuth,
            Port = settings.Port,
            ContextSize = settings.ContextSize,
            GpuLayers = settings.GpuLayers,
            EnableMetrics = settings.EnableMetrics,
            ParallelSlots = settings.ParallelSlots,
            BatchSize = settings.BatchSize,
            MicroBatchSize = settings.MicroBatchSize,
            Threads = settings.Threads,
            FlashAttention = settings.FlashAttention,
            CacheTypeK = settings.CacheTypeK,
            CacheTypeV = settings.CacheTypeV,
            KvOffload = settings.KvOffload,
            KvUnified = settings.KvUnified,
            PromptCacheMode = settings.PromptCacheMode,
            PromptCacheRamMb = settings.PromptCacheRamMb,
            ContextCheckpointsMode = settings.ContextCheckpointsMode,
            ContextCheckpointCount = settings.ContextCheckpointCount,
            ContextCheckpointEveryNTokens = settings.ContextCheckpointEveryNTokens,
            ContinuousBatching = settings.ContinuousBatching,
            ReasoningMode = settings.ReasoningMode,
            ReasoningFormat = settings.ReasoningFormat,
            ReasoningBudget = settings.ReasoningBudget,
            VisionMode = settings.VisionMode,
            VisionProjectorPath = visionProjectorPath,
            VisionProjectorEmbedded = embeddedVisionProjector,
            VisionImageMinTokens = settings.VisionImageMinTokens,
            VisionImageMaxTokens = settings.VisionImageMaxTokens,
            JinjaMode = settings.JinjaMode,
            MmapMode = settings.MmapMode,
            MlockMode = settings.MlockMode,
            Temperature = settings.Temperature,
            TopK = settings.TopK,
            TopP = settings.TopP,
            MinP = settings.MinP,
            MaxTokens = settings.MaxTokens,
            Seed = settings.Seed,
            RepeatLastN = settings.RepeatLastN,
            RepeatPenalty = settings.RepeatPenalty,
            PresencePenalty = settings.PresencePenalty,
            FrequencyPenalty = settings.FrequencyPenalty,
            RopeScaling = settings.RopeScaling,
            RopeScale = settings.RopeScale,
            RopeFreqBase = settings.RopeFreqBase,
            RopeFreqScale = settings.RopeFreqScale,
            SpeculativeType = settings.SpeculativeType,
            SpecDraftModelPath = launchDraftModelPath,
            MtpHeadPath = launchMtpHeadPath,
            SpecDraftGpuLayers = settings.SpecDraftGpuLayers,
            SpecDraftMinTokens = settings.SpecDraftMinTokens,
            SpecDraftMaxTokens = settings.SpecDraftMaxTokens,
            SpecDraftPSplit = settings.SpecDraftPSplit,
            SpecDraftPMin = settings.SpecDraftPMin,
            SpecDraftCacheTypeK = settings.SpecDraftCacheTypeK,
            SpecDraftCacheTypeV = settings.SpecDraftCacheTypeV,
            FlagValues = settings.FlagValues ?? ImmutableDictionary<string, string>.Empty,
            ExtraArgs = extraArgs
        };

        if (_runtimeFlagCapabilityService is not null)
        {
            try
            {
                var supportedFlags = await _runtimeFlagCapabilityService.GetSupportedFlagsAsync(
                    runtime.ExecutablePath,
                    runtime.Mode,
                    runtime.Mode == RuntimeMode.Wsl ? settings.WslDistro : null);
                // An empty set means --help produced no parseable flags (parser miss or a
                // non-standard build), not that the runtime supports nothing. Treat it as
                // "no filtering" so recognized flags (including --model) are not stripped.
                request = request with { SupportedFlags = supportedFlags is { Count: > 0 } ? supportedFlags : null };
            }
            catch
            {
                // If capability detection fails, fall back to sending the full command.
            }
        }

        if (runtime.Mode == RuntimeMode.Wsl)
            request = ConvertFlagValuesForWsl(request);

        _lastApiKey = settings.ModelApiKey ?? "";
        var args = RuntimeAdapter.BuildArgs(request);
        var psi = runtime.Mode == RuntimeMode.Wsl
            ? new ProcessStartInfo(HostExecutableResolver.WslExe())
            : new ProcessStartInfo(runtime.ExecutablePath);
        if (runtime.Mode == RuntimeMode.Wsl)
        {
            var executableDir = WslDirectoryName(executable);
            var runtimeLibDir = WslSiblingDirectory(executableDir, "lib");
            var libraryPath = string.IsNullOrWhiteSpace(executableDir)
                ? "$LD_LIBRARY_PATH"
                : $"{BashQuote(executableDir)}:{BashQuote(runtimeLibDir)}:${{LD_LIBRARY_PATH:-}}";
            var argv0 = string.IsNullOrWhiteSpace(_lastWslProcessMarker) ? "" : $" -a {BashQuote(_lastWslProcessMarker)}";
            var syclEnv = WslSyclEnvironmentPrefix(runtime.Backend);
            var command = $"{syclEnv}export LD_LIBRARY_PATH={libraryPath}; cd {BashQuote(string.IsNullOrWhiteSpace(executableDir) ? "/" : executableDir)}; exec{argv0} {BashQuote(executable)} {string.Join(" ", args.Select(BashQuote))}";
            psi.ArgumentList.Add("-d");
            psi.ArgumentList.Add(settings.WslDistro);
            psi.ArgumentList.Add("--");
            psi.ArgumentList.Add("bash");
            psi.ArgumentList.Add("-lc");
            psi.ArgumentList.Add(command);
        }
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.WindowStyle = ProcessWindowStyle.Hidden;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        // Pass API key via environment variable (LLAMA_API_KEY) instead of CLI arg
        // to keep it out of process command lines visible in Task Manager / WMI.
        if (!string.IsNullOrWhiteSpace(_lastApiKey))
        {
            psi.Environment["LLAMA_API_KEY"] = _lastApiKey;
            // For WSL, tell the interop layer to forward this env var into the Linux session.
            if (runtime.Mode == RuntimeMode.Wsl)
                psi.Environment["WSLENV"] = "LLAMA_API_KEY";
        }
        if (runtime.Mode == RuntimeMode.Native)
        {
            psi.WorkingDirectory = Path.GetDirectoryName(runtime.ExecutablePath) ?? Environment.CurrentDirectory;
            if (runtime.Backend == RuntimeBackend.Sycl)
                ApplyNativeSyclEnvironment(psi);
        }
        if (runtime.Mode == RuntimeMode.Native)
        {
            // Bind native child process to a Windows Job Object with KILL_ON_JOB_CLOSE.
            // If the app crashes or is force-killed, the OS terminates llama-server.exe
            // automatically — no orphaned processes.
            _jobObject?.Dispose();
            _jobObject = new ProcessJobObjectService();
            foreach (var arg in args) psi.ArgumentList.Add(arg);
        }
        _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _process.OutputDataReceived += (_, e) => ObserveOutput(e.Data);
        _process.ErrorDataReceived += (_, e) => ObserveOutput(e.Data);
        _process.Exited += (_, _) =>
        {
            try { LastExitCode = _process?.ExitCode; }
            catch (Exception ex) { Trace.TraceWarning($"Could not read llama process exit code: {ex.Message}"); }
            if (State != LlamaRuntimeState.Stopped)
                State = LlamaRuntimeState.Failed;
        };
        try
        {
            if (!_process.Start())
            {
                State = LlamaRuntimeState.Failed;
                throw new InvalidOperationException("Failed to start llama-server.");
            }
            _jobObject?.AssignProcess(_process.Handle);
        }
        catch
        {
            _jobObject?.Dispose();
            _jobObject = null;
            State = LlamaRuntimeState.Failed;
            throw;
        }
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        ActiveModelId = model.Id;
        ActiveRuntimeId = runtime.Id;
        _lastSettings = settings;
    }

    public bool MarkLoadedIfRunning()
    {
        if (!IsRunning || !TrySetLoadedFromLoading()) return false;
        LastExitCode = null;
        return true;
    }

    private void ObserveOutput(string? line)
    {
        if (LlamaRuntimeOutputObserver.Observe(line, _log, _lastApiKey))
            TrySetLoadedFromLoading();
    }

    private bool TrySetLoadedFromLoading()
        => Interlocked.CompareExchange(
            ref _state,
            (int)LlamaRuntimeState.Loaded,
            (int)LlamaRuntimeState.Loading) == (int)LlamaRuntimeState.Loading;

}
