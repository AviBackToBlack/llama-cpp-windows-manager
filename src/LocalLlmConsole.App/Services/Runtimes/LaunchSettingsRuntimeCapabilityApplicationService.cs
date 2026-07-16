namespace LocalLlmConsole.Services;

/// <summary>Resolves a runtime and queries its supported llama-server flags for the UI.</summary>
public sealed class LaunchSettingsRuntimeCapabilityApplicationService
{
    private readonly RuntimeFlagCapabilityService _runtimeFlagCapabilityService;

    public LaunchSettingsRuntimeCapabilityApplicationService(RuntimeFlagCapabilityService runtimeFlagCapabilityService)
    {
        _runtimeFlagCapabilityService = runtimeFlagCapabilityService ?? throw new ArgumentNullException(nameof(runtimeFlagCapabilityService));
    }

    public async Task<IReadOnlySet<string>?> GetSupportedFlagsAsync(
        string runtimeId,
        Func<Task<IReadOnlyList<RuntimeRecord>>> listRuntimesAsync,
        string? wslDistro)
    {
        if (string.IsNullOrWhiteSpace(runtimeId))
            return null;

        var runtimes = await listRuntimesAsync();
        var runtime = runtimes.FirstOrDefault(r => string.Equals(r.Id, runtimeId, StringComparison.OrdinalIgnoreCase));
        if (runtime is null)
            return null;

        return await _runtimeFlagCapabilityService.GetSupportedFlagsAsync(
            runtime.ExecutablePath,
            runtime.Mode,
            runtime.Mode == RuntimeMode.Wsl ? wslDistro : null);
    }
}
