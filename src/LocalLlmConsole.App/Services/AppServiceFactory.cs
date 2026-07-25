namespace LocalLlmConsole.Services;

public sealed partial class AppServiceFactory
{
    private readonly string _workspaceRoot;
    private readonly Lazy<RuntimeFlagCapabilityService> _sharedRuntimeFlagCapabilityService;

    public AppServiceFactory(string workspaceRoot)
    {
        _workspaceRoot = string.IsNullOrWhiteSpace(workspaceRoot)
            ? throw new ArgumentException("Workspace root is required.", nameof(workspaceRoot))
            : workspaceRoot;
        _sharedRuntimeFlagCapabilityService = new(
            () => new RuntimeFlagCapabilityService(
                new RuntimeFlagHelpRunner(CreateProcessRunner()),
                Path.Combine(_workspaceRoot, "runtime-help-cache")),
            System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private RuntimeFlagCapabilityService GetOrCreateRuntimeFlagCapabilityService() => _sharedRuntimeFlagCapabilityService.Value;

    public string DatabasePath => Path.Combine(_workspaceRoot, "state", "local-llm-console.db");

    public string LogRoot => Path.Combine(_workspaceRoot, "logs");

    public MainWindowInfrastructureServices CreateMainWindowInfrastructureServices()
    {
        var processRunner = CreateProcessRunner();
        return new(
            CreateAppUpdateService(),
            CreateLoadedModelSessionManager(processRunner),
            processRunner,
            CreateWindowsEnvironmentService(),
            CreateWslEnvironmentService(),
            CreateRuntimeProbeClient(),
            CreateRuntimeMetricsClient(),
            CreateRuntimePackageClient());
    }
}
