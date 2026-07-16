namespace LocalLlmConsole.Services;

public sealed partial class AppServiceFactory
{
    private readonly string _workspaceRoot;
    private RuntimeFlagCapabilityService? _sharedRuntimeFlagCapabilityService;

    public AppServiceFactory(string workspaceRoot)
    {
        _workspaceRoot = string.IsNullOrWhiteSpace(workspaceRoot)
            ? throw new ArgumentException("Workspace root is required.", nameof(workspaceRoot))
            : workspaceRoot;
    }

    private RuntimeFlagCapabilityService GetOrCreateRuntimeFlagCapabilityService()
    {
        if (_sharedRuntimeFlagCapabilityService is null)
        {
            var helpRunner = new RuntimeFlagHelpRunner(CreateProcessRunner());
            _sharedRuntimeFlagCapabilityService = new RuntimeFlagCapabilityService(helpRunner, Path.Combine(_workspaceRoot, "runtime-help-cache"));
        }
        return _sharedRuntimeFlagCapabilityService;
    }

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
