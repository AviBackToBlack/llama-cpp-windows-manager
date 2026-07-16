using System.Windows;

namespace LocalLlmConsole;

public partial class MainWindow
{
    private UIElement CreateLaunchSettingsPanel()
    {
        var panel = LaunchSettingsPanelFactory.Create(new LaunchSettingsPanelRequest(
            _settings,
            _viewModel.LaunchSettings.RuntimeChoices,
            _coreServices.Ui.AdvancedSections.ShowLaunchSettings,
            () =>
            {
                RunBackground(ApplyRuntimeCapabilitiesAsync, "Runtime capability detection failed");
                UpdateLaunchControlVisibility();
                UpdateLaunchSaveButtonState();
            },
            showAdvanced =>
            {
                _coreServices.Ui.AdvancedSections.SetLaunchSettings(showAdvanced);
                UpdateLaunchControlVisibility();
            },
            UpdateLaunchControlVisibility,
            SaveLaunchSettingsForSelectedModelAsync,
            SaveLaunchDefaultsFromControlsAsync,
            ResetLaunchSettingsToDefaults,
            SaveLaunchSettingsAsNewModelAsync,
            ChooseVisionProjectorPathAsync,
            ChooseMtpHeadPathAsync,
            UpdateLaunchSaveButtonState));

        ApplyLaunchSettingsPanelControls(panel);
        AttachLaunchSettingsChangeHandlers();
        ApplyLaunchSettingsToControls();
        RunBackground(() => RenderSelectedModelLaunchSettingsAsync(), "Launch settings refresh failed");
        return panel.Root;
    }

    private void ApplyLaunchSettingsPanelControls(LaunchSettingsPanelControls panel)
    {
        _launchSettingsPanel.Apply(panel);
    }
}
