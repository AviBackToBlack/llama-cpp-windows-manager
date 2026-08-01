using System.ComponentModel;
using System.Windows.Controls;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;

namespace LocalLlmConsole;

public sealed class SettingsPageState
{
    private readonly List<EditableSettingRow> _rows = [];
    private IReadOnlyDictionary<string, string> _savedValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private string _savedTheme = "";

    public DataGrid? SettingsGrid { get; private set; }

    private WpfComboBox? ThemeCombo { get; set; }

    private WpfButton? SaveButton { get; set; }

    public string SelectedThemeValue
        => ThemeCombo?.SelectedItem?.ToString() ?? ThemeCombo?.Text ?? "";

    public bool HasUnsavedChanges => CalculateHasUnsavedChanges();

    public void Apply(
        SettingsPageControls controls,
        IEnumerable<EditableSettingRow> rows,
        IReadOnlyDictionary<string, string> savedValues,
        string savedTheme)
    {
        ArgumentNullException.ThrowIfNull(controls);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(savedValues);

        DetachChangeHandlers();

        ThemeCombo = controls.ThemeCombo;
        SaveButton = controls.SaveButton;
        SettingsGrid = controls.SettingsGrid;
        _rows.AddRange(rows);
        _savedValues = new Dictionary<string, string>(savedValues, StringComparer.OrdinalIgnoreCase);
        _savedTheme = AppPreferenceService.ThemeMode(savedTheme);

        ThemeCombo.SelectionChanged += ThemeSelectionChanged;
        foreach (var row in _rows)
            row.PropertyChanged += SettingRowPropertyChanged;
        UpdateSaveButtonState();
    }

    private void ThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
        => UpdateSaveButtonState();

    private void SettingRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditableSettingRow.Value))
            UpdateSaveButtonState();
    }

    private bool CalculateHasUnsavedChanges()
    {
        if (!string.Equals(
                AppPreferenceService.ThemeMode(SelectedThemeValue),
                _savedTheme,
                StringComparison.OrdinalIgnoreCase))
            return true;

        return _rows.Any(row =>
            !_savedValues.TryGetValue(row.Key, out var saved)
            || !string.Equals(row.Value, saved, StringComparison.Ordinal));
    }

    private void UpdateSaveButtonState()
    {
        if (SaveButton is null) return;
        SaveButton.IsEnabled = HasUnsavedChanges;
        SaveButton.ToolTip = SaveButton.IsEnabled
            ? "Save the changed application preferences."
            : "No unsaved settings changes.";
    }

    private void DetachChangeHandlers()
    {
        if (ThemeCombo is not null)
            ThemeCombo.SelectionChanged -= ThemeSelectionChanged;
        foreach (var row in _rows)
            row.PropertyChanged -= SettingRowPropertyChanged;
        _rows.Clear();
    }
}
