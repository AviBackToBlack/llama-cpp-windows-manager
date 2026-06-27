
namespace LocalLlmConsole.ViewModels;

public sealed class UpdatesPageViewModel : ObservableViewModel
{
    private AppUpdateInfo? _latestUpdate;
    private bool _checkInFlight;

    public AppUpdateInfo? LatestUpdate
    {
        get => _latestUpdate;
        private set
        {
            if (!SetProperty(ref _latestUpdate, value)) return;
            OnPropertyChanged(nameof(HasAvailableUpdate));
            OnPropertyChanged(nameof(ActionText));
            OnPropertyChanged(nameof(NavigationText));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusDetails));
            OnPropertyChanged(nameof(LatestReleaseText));
        }
    }

    public bool CheckInFlight
    {
        get => _checkInFlight;
        set => SetProperty(ref _checkInFlight, value);
    }

    public bool HasAvailableUpdate => LatestUpdate is { IsAvailable: true };
    public string ActionText => HasAvailableUpdate
        ? Localization.Loc.T("Nav.InstallUpdate")
        : Localization.Loc.T("Nav.CheckForUpdates");
    public string NavigationText => ActionText;

    public string StatusText => LatestUpdate is null
        ? Localization.Loc.T("Updates.NoCheckYet")
        : LatestUpdate.IsAvailable
            ? Localization.Loc.T("Updates.Available", LatestUpdate.CurrentVersion, LatestUpdate.LatestVersion)
            : Localization.Loc.T("Updates.NoUpdatesAvailable", LatestUpdate.CurrentVersion);

    public string StatusDetails => $"{StatusText}\n{Localization.Loc.T("Updates.RepositoryLabel")}: {AppUpdateService.RepositoryUrl}";

    public string LatestReleaseText => LatestUpdate is { IsAvailable: true } update
        ? $"{update.ReleaseName}\n{update.HtmlUrl}\n\n{DisplayFormatService.TrimForDisplay(update.ReleaseNotes, 1800)}"
        : "";

    public void SetLatestUpdate(AppUpdateInfo update) => LatestUpdate = update;
}
