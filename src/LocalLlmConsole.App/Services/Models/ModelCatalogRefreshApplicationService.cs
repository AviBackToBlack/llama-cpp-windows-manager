namespace LocalLlmConsole.Services;

public sealed record ModelCatalogRefreshApplicationActions(
    Func<IReadOnlyList<ModelRecord>, Task<IReadOnlyList<NamedModelLaunchProfile>>> EnsureDefaultProfilesAsync);

public sealed record ModelCatalogRefreshApplicationResult(
    IReadOnlyList<ModelRecord> Models,
    IReadOnlyDictionary<string, ModelLaunchSettings> LaunchProfiles,
    IReadOnlyList<NamedModelLaunchProfile> NamedLaunchProfiles)
{
    public ModelLaunchSettings? LaunchProfileFor(ModelRecord model)
        => LaunchProfiles.TryGetValue(model.Id, out var profile) ? profile : null;
}

public sealed class ModelCatalogRefreshApplicationService
{
    private readonly StateStore _stateStore;
    private readonly ModelCatalogService _catalog;

    public ModelCatalogRefreshApplicationService(StateStore stateStore, ModelCatalogService catalog)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public async Task<ModelCatalogRefreshApplicationResult> RefreshAsync(
        ModelCatalogRefreshApplicationActions actions,
        CancellationToken cancellationToken = default)
    {
        Validate(actions);

        await _catalog.CleanupModelRecordsAsync();
        var models = await _stateStore.ListModelsAsync();
        var defaults = await actions.EnsureDefaultProfilesAsync(models);
        var profiles = new Dictionary<string, ModelLaunchSettings>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in defaults)
        {
            cancellationToken.ThrowIfCancellationRequested();
            profiles[profile.ModelId] = profile.Settings;
        }

        var namedProfiles = await _stateStore.ListNamedModelLaunchProfilesAsync();
        return new ModelCatalogRefreshApplicationResult(models, profiles, namedProfiles);
    }

    private static void Validate(ModelCatalogRefreshApplicationActions actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(actions.EnsureDefaultProfilesAsync);
    }
}
