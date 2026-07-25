namespace LocalLlmConsole.Services;

/// <summary>Resolves a runtime and queries its supported llama-server flags for the UI.</summary>
public sealed class LaunchSettingsRuntimeCapabilityApplicationService
{
    private readonly RuntimeFlagCapabilityService _runtimeFlagCapabilityService;

    public LaunchSettingsRuntimeCapabilityApplicationService(RuntimeFlagCapabilityService runtimeFlagCapabilityService)
    {
        _runtimeFlagCapabilityService = runtimeFlagCapabilityService ?? throw new ArgumentNullException(nameof(runtimeFlagCapabilityService));
    }

    public async Task<RuntimeFlagCapabilityResult?> GetCapabilitiesAsync(
        string runtimeId,
        Func<Task<IReadOnlyList<RuntimeRecord>>> listRuntimesAsync,
        string? wslDistro,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runtimeId))
            return null;

        var runtimes = await listRuntimesAsync();
        cancellationToken.ThrowIfCancellationRequested();
        var runtime = runtimes.FirstOrDefault(r => string.Equals(r.Id, runtimeId, StringComparison.OrdinalIgnoreCase));
        if (runtime is null)
            return null;

        return await _runtimeFlagCapabilityService.GetCapabilitiesAsync(
            runtime.ExecutablePath,
            runtime.Mode,
            runtime.Mode == RuntimeMode.Wsl ? wslDistro : null,
            cancellationToken);
    }

    public async Task<IReadOnlySet<string>?> GetSupportedFlagsAsync(
        string runtimeId,
        Func<Task<IReadOnlyList<RuntimeRecord>>> listRuntimesAsync,
        string? wslDistro)
        => (await GetCapabilitiesAsync(runtimeId, listRuntimesAsync, wslDistro))?.Supported;
}

public readonly record struct RuntimeCapabilityRequest(long Version, string RuntimeId);

/// <summary>Rejects late runtime-capability completions, including repeated requests for the same runtime id.</summary>
public sealed class RuntimeCapabilityRequestCoordinator
{
    private long _version;

    public RuntimeCapabilityRequest Begin(string? runtimeId)
        => new(Interlocked.Increment(ref _version), runtimeId ?? "");

    public bool IsCurrent(RuntimeCapabilityRequest request, string? selectedRuntimeId)
        => request.Version == Volatile.Read(ref _version)
            && string.Equals(request.RuntimeId, selectedRuntimeId ?? "", StringComparison.OrdinalIgnoreCase);
}
