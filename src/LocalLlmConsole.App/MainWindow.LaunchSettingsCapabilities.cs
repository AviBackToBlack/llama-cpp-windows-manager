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
    private readonly RuntimeCapabilityRequestCoordinator _runtimeCapabilityRequests = new();

    private async Task ApplyModelCapabilitiesAsync(ModelRecord? model, CancellationToken cancellationToken = default)
    {
        var capabilities = model is null
            ? ModelCapabilityService.Empty()
            : await CachedModelCapabilitiesAsync(model, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (model is not null && !string.Equals(SelectedModel()?.Id, model.Id, StringComparison.OrdinalIgnoreCase)) return;

        var capabilityState = _coreServices.Ui.SelectedCapabilities.Apply(model, capabilities);
        if (_launchSettingsPanel.ModelCapabilityText is not null)
        {
            _launchSettingsPanel.ModelCapabilityText.Text = capabilityState.DisplayText;
            _launchSettingsPanel.ModelCapabilityText.ToolTip = TooltipText(capabilityState.DisplayText);
        }
        UpdateLaunchControlVisibility();
    }

    private async Task<ModelCapabilitySummary> CachedModelCapabilitiesAsync(ModelRecord model, CancellationToken cancellationToken = default)
        => await _coreServices.Models.ModelCapabilities.ReadAsync(model, cancellationToken);

    private async Task ApplyRuntimeCapabilitiesAsync()
    {
        var runtimeId = SelectedLaunchRuntimeId();
        var request = _runtimeCapabilityRequests.Begin(runtimeId);
        if (string.IsNullOrWhiteSpace(runtimeId) || _appServices is null)
        {
            _launchSettingsPanel.SetRuntimeCapabilities(null);
            UpdateLaunchControlVisibility();
            UpdateLaunchSaveButtonState();
            return;
        }

        try
        {
            var capabilities = await _coreServices.Models.LaunchSettingsRuntimeCapabilities.GetCapabilitiesAsync(
                runtimeId,
                _appServices.StateStore.ListRuntimesAsync,
                _settings.WslDistro);

            if (!_runtimeCapabilityRequests.IsCurrent(request, SelectedLaunchRuntimeId()))
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                if (!_runtimeCapabilityRequests.IsCurrent(request, SelectedLaunchRuntimeId()))
                    return;

                _launchSettingsPanel.SetRuntimeCapabilities(capabilities);
                UpdateLaunchControlVisibility();
                UpdateLaunchSaveButtonState();
            });
        }
        catch (Exception ex)
        {
            if (!_runtimeCapabilityRequests.IsCurrent(request, SelectedLaunchRuntimeId()))
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                if (!_runtimeCapabilityRequests.IsCurrent(request, SelectedLaunchRuntimeId()))
                    return;

                _launchSettingsPanel.SetRuntimeCapabilities(null);
                UpdateLaunchControlVisibility();
                UpdateLaunchSaveButtonState();
            });
            SetStatus($"Runtime capability detection failed: {ex.Message}");
        }
    }

    private void UpdateLaunchControlVisibility()
    {
        var plan = _coreServices.Models.LaunchSettingsControlStates.Build(new LaunchSettingsControlStateRequest(
            _coreServices.Ui.AdvancedSections.ShowLaunchSettings,
            SelectedLaunchRuntimeBackend(),
            _coreServices.Ui.SelectedCapabilities.VisionLaunchSettingsAvailable,
            ComboValue(_launchSettingsPanel.FormControls.SpeculativeTypeCombo)));

        _launchSettingsPanel.ApplyControlState(plan);
    }
}
