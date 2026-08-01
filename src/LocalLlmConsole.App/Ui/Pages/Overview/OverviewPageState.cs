using System.Windows.Controls;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;

namespace LocalLlmConsole;

public sealed class OverviewPageState
{
    public WpfComboBox? ModelCombo { get; private set; }

    public WpfComboBox? LaunchProfileCombo { get; private set; }

    public WpfButton? LoadButton { get; private set; }

    public DataGrid? LoadedSessionsGrid { get; private set; }

    public UiRow? SelectedLoadedSessionRow => LoadedSessionsGrid?.SelectedItem as UiRow;

    public string SelectedLoadedSessionId => SelectedLoadedSessionRow?.Data["SessionId"]?.ToString() ?? "";

    public void Apply(OverviewPageControls controls)
    {
        ArgumentNullException.ThrowIfNull(controls);

        ModelCombo = controls.ModelCombo;
        LaunchProfileCombo = controls.LaunchProfileCombo;
        LoadButton = controls.LoadButton;
        LoadedSessionsGrid = controls.LoadedSessionsGrid;
    }

    public void FocusLoadedSessionsGrid()
        => LoadedSessionsGrid?.Focus();

    public void FocusModelCombo()
        => ModelCombo?.Focus();

    public ModelRecord? SelectedModel(IReadOnlyList<ModelRecord> modelChoices)
    {
        ArgumentNullException.ThrowIfNull(modelChoices);

        if (ModelCombo?.SelectedItem is ModelRecord model)
            return model;
        if (ModelCombo?.SelectedValue is string selectedId)
            return modelChoices.FirstOrDefault(item => string.Equals(item.Id, selectedId, StringComparison.OrdinalIgnoreCase));
        return null;
    }

    public string SelectedLaunchProfileId => LaunchProfileCombo?.SelectedValue?.ToString() ?? "";

    public string SelectedLaunchProfileName
        => (LaunchProfileCombo?.SelectedItem as OverviewLaunchProfileChoice)?.Name ?? "";

    public void SelectLaunchProfile(string? profileId)
    {
        if (LaunchProfileCombo is null) return;
        LaunchProfileCombo.SelectedValue = profileId ?? "";
        if (LaunchProfileCombo.SelectedIndex < 0 && LaunchProfileCombo.Items.Count > 0)
            LaunchProfileCombo.SelectedIndex = 0;
    }

    public void SelectModelChoice(string? selectedId, IReadOnlyList<ModelRecord> modelChoices)
    {
        ArgumentNullException.ThrowIfNull(modelChoices);
        if (ModelCombo is null) return;

        if (modelChoices.Count == 0)
        {
            ModelCombo.SelectedIndex = -1;
            return;
        }

        var match = modelChoices.FirstOrDefault(model => string.Equals(model.Id, selectedId, StringComparison.OrdinalIgnoreCase))
            ?? modelChoices.First();
        ModelCombo.SelectedValue = match.Id;
    }

    public void SelectModelId(string modelId)
    {
        if (ModelCombo is not null)
            ModelCombo.SelectedValue = modelId;
    }

    public void SetModelActionsEnabled(bool hasSelection, bool hasProfileSelection, bool selectedModelLoaded)
    {
        if (LoadButton is not null)
        {
            var canLoad = hasSelection && hasProfileSelection && !selectedModelLoaded;
            LoadButton.IsEnabled = canLoad;
            LoadButton.Visibility = canLoad ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
    }

    public void RestoreLoadedSessionSelection(string sessionId, IReadOnlyList<UiRow> sessionRows)
    {
        ArgumentNullException.ThrowIfNull(sessionRows);
        if (LoadedSessionsGrid is null) return;

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            LoadedSessionsGrid.SelectedItem = sessionRows.FirstOrDefault(row =>
                string.Equals(row.Data["SessionId"]?.ToString(), sessionId, StringComparison.OrdinalIgnoreCase));
        }

        LoadedSessionsGrid.Items.Refresh();
    }
}
