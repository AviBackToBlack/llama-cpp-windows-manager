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
    private async Task LoadSelectedModelAsync(bool restart)
    {
        var model = SelectedModel();
        await _coreServices.Models.ModelRuntimeLoadApplication.LoadSelectedAsync(
            new SelectedModelRuntimeLoadApplicationRequest(
                model,
                restart,
                IsModelLoaded(model),
                IsModelActive(model),
                model is not null && _coreServices.Ui.LaunchSettingsEditor.IsLoadedFor(model.Id, SelectedModelLaunchProfileId()),
                SelectedLaunchRuntimeId(),
                SelectedRuntime()),
            ModelRuntimeLoadActions(
                ReadLaunchSettingsFromControls,
                SelectedModelLaunchProfileId(),
                _modelsPage.SelectedLaunchProfile?.Name ?? ""));
    }

    private async Task UnloadSelectedModelAsync()
    {
        var model = SelectedModel();
        await _coreServices.Models.ModelRuntimeUnloadApplication.UnloadSelectedAsync(
            new ModelRuntimeUnloadApplicationRequest(model, IsModelLoaded(model)),
            ModelRuntimeUnloadActions());
    }

    private async Task LoadOverviewSelectedModelAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedOverviewLaunchProfileId()))
        {
            SetStatus("Select a launch profile before loading the model.");
            return;
        }
        await LoadOverviewModelAsync(SelectedOverviewModel());
    }

    private async Task LoadOverviewModelAsync(ModelRecord? model)
    {
        await _coreServices.Models.ModelRuntimeLoadApplication.LoadOverviewAsync(
            new OverviewModelRuntimeLoadApplicationRequest(
                model,
                IsModelLoaded(model),
                IsModelActive(model),
                AppReady: true),
            ModelRuntimeLoadActions(
                () => _settings,
                SelectedOverviewLaunchProfileId(),
                _overviewPage.SelectedLaunchProfileName));
    }

    private ModelRuntimeLoadApplicationActions ModelRuntimeLoadActions(
        Func<AppSettings> readLaunchSettings,
        string launchProfileId,
        string launchProfileName)
        => new(
            RunResponsiveAsync,
            SwitchToLoadedModelAsync,
            () => RenderSelectedModelLaunchSettingsAsync(),
            readLaunchSettings,
            ListRuntimesAsync,
            model => DraftModelLaunchProfileAsync(model, SelectedOverviewLaunchProfileId()),
            StopModelRuntimeAsync,
            (runtime, model, launchSettings) => StartModelRuntimeAsync(
                runtime,
                model,
                launchSettings,
                launchProfileId: launchProfileId,
                launchProfileName: launchProfileName),
            SetStatus);

    private async Task<IReadOnlyList<RuntimeRecord>> ListRuntimesAsync()
        => await AppServices.StateStore.ListRuntimesAsync();

    private ModelRuntimeUnloadApplicationActions ModelRuntimeUnloadActions()
        => new(StopModelRuntimeAsync, SetStatus);

}
