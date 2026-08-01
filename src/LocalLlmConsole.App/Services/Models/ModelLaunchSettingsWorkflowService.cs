namespace LocalLlmConsole.Services;

public sealed record ModelLaunchSettingsViewState(
    string ModelId,
    ModelLaunchSettings? SavedProfile,
    bool HasSavedProfile,
    string RuntimeId,
    AppSettings LaunchSettings,
    string ProfileId = "",
    string ProfileName = "");

public sealed record ModelLaunchSettingsSaveResult(
    ModelLaunchSettings SavedSettings,
    string StatusMessage,
    string ProfileId = "");

public sealed record LaunchDefaultsSaveResult(
    AppSettings Settings,
    string StatusMessage);

public sealed class ModelLaunchSettingsWorkflowService
{
    private readonly ModelLaunchProfileService _profiles;

    public ModelLaunchSettingsWorkflowService(ModelLaunchProfileService profiles)
    {
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
    }

    public async Task<ModelLaunchSettingsViewState> BuildAsync(
        ModelRecord model,
        AppSettings defaults,
        CancellationToken cancellationToken = default,
        string profileId = "")
    {
        ArgumentNullException.ThrowIfNull(model);
        cancellationToken.ThrowIfCancellationRequested();

        var profile = await _profiles.ReadAsync(model, profileId);
        cancellationToken.ThrowIfCancellationRequested();

        var effective = profile ?? await _profiles.DraftAsync(model, defaults, profileId);
        cancellationToken.ThrowIfCancellationRequested();

        var profileName = "";
        if (!string.IsNullOrWhiteSpace(profileId))
            profileName = (await _profiles.ListNamedAsync(model)).FirstOrDefault(item =>
                string.Equals(item.Id, profileId, StringComparison.OrdinalIgnoreCase))?.Name ?? "";

        return new ModelLaunchSettingsViewState(
            model.Id,
            profile,
            profile is not null,
            effective.RuntimeId,
            effective.ApplyTo(defaults),
            profileId,
            profileName);
    }

    public async Task<ModelLaunchSettings?> EnsureAsync(
        ModelRecord model,
        AppSettings defaults,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _profiles.EnsureAsync(model, defaults);
    }

    public async Task<ModelLaunchSettings> SaveForModelAsync(
        ModelRecord model,
        AppSettings launchSettings,
        string runtimeId,
        CancellationToken cancellationToken = default,
        string profileId = "")
    {
        cancellationToken.ThrowIfCancellationRequested();
        var saved = ModelLaunchSettings.FromAppSettings(launchSettings, runtimeId);
        if (string.IsNullOrWhiteSpace(profileId))
        {
            await _profiles.SaveAsync(model, saved);
        }
        else
        {
            var profile = (await _profiles.ListNamedAsync(model)).FirstOrDefault(item =>
                string.Equals(item.Id, profileId, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("The selected launch profile no longer exists.");
            await _profiles.SaveNamedAsync(profile with { Settings = saved, UpdatedAt = DateTimeOffset.UtcNow });
        }
        cancellationToken.ThrowIfCancellationRequested();
        return saved;
    }

    public async Task<ModelLaunchSettingsSaveResult> SaveProfileAsync(
        ModelRecord model,
        AppSettings launchSettings,
        string runtimeId,
        CancellationToken cancellationToken = default,
        string profileId = "")
    {
        var saved = await SaveForModelAsync(model, launchSettings, runtimeId, cancellationToken, profileId);
        var profileName = string.IsNullOrWhiteSpace(profileId)
            ? "default profile"
            : (await _profiles.ListNamedAsync(model)).FirstOrDefault(item =>
                string.Equals(item.Id, profileId, StringComparison.OrdinalIgnoreCase))?.Name ?? "launch profile";
        return new ModelLaunchSettingsSaveResult(
            saved,
            $"Saved {profileName} for {model.Name}.",
            profileId);
    }

    public static AppSettings ApplyLaunchDefaults(AppSettings currentSettings, AppSettings launchDefaults)
        => launchDefaults with { Port = currentSettings.Port };

    public static LaunchDefaultsSaveResult SaveLaunchDefaults(AppSettings currentSettings, AppSettings launchDefaults)
        => new(
            ApplyLaunchDefaults(currentSettings, launchDefaults),
            "Launch defaults saved. Model ports stay per-model.");
}
