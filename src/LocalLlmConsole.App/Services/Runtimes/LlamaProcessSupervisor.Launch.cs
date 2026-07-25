namespace LocalLlmConsole.Services;

public sealed partial class LlamaProcessSupervisor : IDisposable
{
    private static string? ResolveDraftModelPath(string modelPath, string configuredDraftPath, string speculativeType)
    {
        if (!speculativeType.StartsWith("draft-", StringComparison.OrdinalIgnoreCase))
            return null;
        if (!string.IsNullOrWhiteSpace(configuredDraftPath))
            return configuredDraftPath.Trim();
        return ModelCatalogService.FindDraftModel(modelPath);
    }

    private static string? ResolveMtpHeadPath(string modelPath, string configuredHeadPath, string speculativeType)
    {
        if (!LaunchSettingMetadataService.IsAtomicMtpSpeculativeType(speculativeType))
            return null;
        if (!string.IsNullOrWhiteSpace(configuredHeadPath))
            return configuredHeadPath.Trim();
        return ModelCatalogService.FindDraftModel(modelPath);
    }

    private IReadOnlyList<string> BuildArgsWithDroppedFlagLogging(RuntimeLaunchRequest request)
    {
        var droppedFlags = new List<string>();
        var args = RuntimeAdapter.BuildArgs(request, droppedFlags);
        if (droppedFlags.Count > 0)
        {
            // The command preview intentionally shows the unfiltered command; log what
            // capability filtering removed so the divergence is visible somewhere.
            _log?.WriteLine(
                "[launcher] Omitted flags not advertised by this runtime's --help: "
                + string.Join(" ", droppedFlags.Distinct(StringComparer.OrdinalIgnoreCase)));
        }
        return args;
    }
}
