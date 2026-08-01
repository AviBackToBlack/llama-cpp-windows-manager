using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Forms = System.Windows.Forms;
using WpfApplication = System.Windows.Application;
using WpfBinding = System.Windows.Data.Binding;
using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfProgressBar = System.Windows.Controls.ProgressBar;
using WpfTextBox = System.Windows.Controls.TextBox;
namespace LocalLlmConsole;

public partial class MainWindow
{
    private async Task RefreshOverviewModelSelectorAsync()
    {
        var modelLookup = AppServices.ModelLookupApplication;
        if (modelLookup is null) return;
        await RefreshOverviewModelChoicesAsync(await modelLookup.ListAsync());
    }

    private async Task RefreshOverviewModelChoicesAsync(IReadOnlyList<ModelRecord> models)
    {
        var selectedId = SelectedOverviewModel()?.Id;
        if (string.IsNullOrWhiteSpace(selectedId))
            selectedId = _sessions.SelectedSnapshot()?.ModelId;

        _viewModel.Overview.ReplaceModels(models);
        _overviewPage.SelectModelChoice(selectedId, _viewModel.Overview.ModelChoices);

        await RefreshOverviewLaunchProfilesAsync();

        UpdateOverviewModelActions();
    }

    private async Task RefreshOverviewLaunchProfilesAsync()
    {
        var selectedProfileId = _overviewPage.SelectedLaunchProfileId;
        var model = SelectedOverviewModel();
        IReadOnlyList<NamedModelLaunchProfile> profiles = model is null || ModelServices.LaunchProfiles is null
            ? []
            : await ModelServices.LaunchProfiles.ListNamedAsync(model);
        if (model is not null && ModelServices.LaunchProfiles is not null && profiles.Count == 0)
            profiles = [await ModelServices.LaunchProfiles.EnsureDefaultAsync(model, _settings)];
        _viewModel.Overview.ReplaceLaunchProfiles(profiles);
        _overviewPage.SelectLaunchProfile(selectedProfileId);
    }

    private string SelectedOverviewLaunchProfileId()
        => _overviewPage.SelectedLaunchProfileId;

    private Task SelectOverviewLaunchProfileAsync()
    {
        UpdateOverviewModelActions();
        return Task.CompletedTask;
    }

    private ModelRecord? SelectedOverviewModel()
    {
        return _overviewPage.SelectedModel(_viewModel.Overview.ModelChoices);
    }

    private void UpdateOverviewModelActions()
    {
        var model = SelectedOverviewModel();
        var hasSelection = model is not null;
        var hasProfileSelection = !string.IsNullOrWhiteSpace(SelectedOverviewLaunchProfileId());
        var selectedModelLoaded = IsModelLoaded(model);
        _overviewPage.SetModelActionsEnabled(hasSelection, hasProfileSelection, selectedModelLoaded);
    }

    private static string LoadedSessionIdFromRowButton(object sender)
        => (sender as FrameworkElement)?.Tag is UiRow row
            ? row.Data["SessionId"]?.ToString() ?? ""
            : "";

    private async Task UnloadLoadedSessionAsync(string sessionId)
    {
        var session = _sessions.Snapshots().FirstOrDefault(item => string.Equals(
            item.SessionId,
            sessionId,
            StringComparison.OrdinalIgnoreCase));
        if (session is null) return;

        var model = await FindModelByIdAsync(session.ModelId);
        if (model is null)
        {
            SetStatus("The selected loaded model is no longer present in the catalog.");
            return;
        }

        await _coreServices.Models.ModelRuntimeUnloadApplication.UnloadOverviewAsync(
            new ModelRuntimeUnloadApplicationRequest(model, session.IsRunning),
            ModelRuntimeUnloadActions());
    }

    private async Task SelectOverviewModelSessionAsync()
    {
        if (_coreServices.Ui.SelectionReentrancy.IsLoadedSessionSelectionChanging) return;

        var model = SelectedOverviewModel();
        await RefreshOverviewLaunchProfilesAsync();
        await _coreServices.Runtime.OverviewModelSelectionApplication.SelectAsync(
            new OverviewModelSelectionApplicationRequest(
                model,
                IsModelLoaded(model),
                IsModelActive(model)),
            OverviewModelSelectionActions());
    }

    private OverviewModelSelectionApplicationActions OverviewModelSelectionActions()
        => new(
            _coreServices.Runtime.RuntimeSessions.SelectModel,
            settings => _activeRuntimeSettings = settings,
            SaveActiveRuntimeSessionsAsync,
            RefreshRuntimeMetricsAsync,
            SetStatus);

    private async Task SelectLoadedSessionRowAsync()
    {
        if (_overviewPage.SelectedLoadedSessionRow is not { } row) return;

        var modelId = row.Data["ModelId"]?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(modelId)) return;

        using var selectionScope = _coreServices.Ui.SelectionReentrancy.TryBeginLoadedSessionSelection();
        if (selectionScope is null) return;

        await _coreServices.Runtime.OverviewLoadedSessionSelectionApplication.SelectAsync(
            modelId,
            OverviewLoadedSessionSelectionActions());
    }

    private OverviewLoadedSessionSelectionApplicationActions OverviewLoadedSessionSelectionActions()
        => new(
            FindOverviewModelChoice,
            RefreshOverviewModelSelectorAsync,
            _overviewPage.SelectModelId,
            _coreServices.Runtime.RuntimeSessions.SelectModel,
            settings => _activeRuntimeSettings = settings,
            SaveActiveRuntimeSessionsAsync,
            RefreshRuntimeMetricsAsync,
            UpdateOverviewModelActions,
            SetStatus);

    private ModelRecord? FindOverviewModelChoice(string modelId)
        => _viewModel.Overview.ModelChoices.FirstOrDefault(item => string.Equals(item.Id, modelId, StringComparison.OrdinalIgnoreCase));

    private async Task<bool> SelectOverviewLoadedModelAsync(string modelId)
    {
        if (_viewModel.CurrentPage != "Overview" || string.IsNullOrWhiteSpace(modelId))
            return false;

        if (FindOverviewModelChoice(modelId) is null)
            await RefreshOverviewModelSelectorAsync();

        using (_coreServices.Ui.SelectionReentrancy.SuppressLoadedSessionSelection())
            _overviewPage.SelectModelId(modelId);

        var selection = _coreServices.Runtime.RuntimeSessions.SelectModel(modelId);
        if (!selection.Selected)
            return false;

        _activeRuntimeSettings = selection.ActiveSettings;
        UpdateOverviewModelActions();
        return true;
    }

    private async Task<(string Model, string Runtime)> ActiveRuntimeLabelsAsync()
    {
        await Task.CompletedTask;
        var selectedModel = SelectedOverviewModel();
        var active = selectedModel is null
            ? _sessions.SelectedSnapshot()
            : _sessions.SessionForModel(selectedModel.Id);
        var labels = _coreServices.Runtime.RuntimeOverviewStatus.Labels(new RuntimeOverviewStatusRequest(
            selectedModel,
            active,
            _llama.State,
            _llama.LastExitCode));
        return (labels.Model, labels.Runtime);
    }

    private async Task<string> ActiveModelDisplayNameAsync(string modelId)
    {
        var modelLookup = AppServices.ModelLookupApplication;
        return modelLookup is null
            ? (string.IsNullOrWhiteSpace(modelId) ? "Unknown model" : modelId)
            : await modelLookup.DisplayNameAsync(modelId);
    }
}
