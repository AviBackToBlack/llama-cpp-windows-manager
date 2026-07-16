using System.Windows;
using System.Windows.Controls;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace LocalLlmConsole;

/// <summary>Creates WPF controls for a <see cref="LlamaServerFlag"/> and reads/writes their values.</summary>
public static class LaunchSettingsControlFactory
{
    public static FrameworkElement CreateControl(LlamaServerFlag flag, string? currentValue)
    {
        FrameworkElement control = flag.ValueType switch
        {
            FlagValueType.Boolean or FlagValueType.Enum => CreateComboBox(flag, currentValue),
            _ => CreateTextBox(flag, currentValue)
        };

        control.Tag = flag.PrimaryName;
        return control;
    }

    public static void SetControlValue(FrameworkElement control, string? value)
    {
        if (FindEditor(control) is WpfTextBox textBox)
        {
            textBox.Text = value ?? "";
        }
        else if (control is WpfComboBox comboBox)
        {
            SetComboValue(comboBox, value ?? "");
        }
    }

    public static string? GetControlValue(FrameworkElement control)
    {
        if (FindEditor(control) is WpfTextBox textBox) return textBox.Text.Trim();
        if (control is WpfComboBox comboBox) return (comboBox.SelectedItem?.ToString() ?? comboBox.Text ?? "").Trim().ToLowerInvariant();
        return null;
    }

    public static UIElement? FindEditor(FrameworkElement control)
    {
        if (control is WpfTextBox or WpfComboBox) return control;
        if (control is Grid grid)
        {
            return grid.Children.OfType<WpfTextBox>().FirstOrDefault();
        }
        return null;
    }

    private static WpfTextBox CreateTextBox(LlamaServerFlag flag, string? currentValue) => new()
    {
        Text = currentValue ?? (flag.Default?.ToString() ?? ""),
        MinHeight = 29,
        MinWidth = 72,
        Margin = new Thickness(0, 0, 4, 2),
        HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
        ToolTip = LaunchSettingMetadataService.Tooltip(flag)
    };

    private static WpfComboBox CreateComboBox(LlamaServerFlag flag, string? currentValue)
    {
        var values = flag.ValueType == FlagValueType.Boolean
            ? (flag.AllowedValues ?? new[] { "on", "off", "auto" })
            : (flag.AllowedValues ?? Array.Empty<string>());

        var combo = new WpfComboBox
        {
            ItemsSource = values.ToArray(),
            SelectedIndex = 0,
            MinHeight = 27,
            MinWidth = 76,
            Margin = new Thickness(0, 0, 6, 4),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            ToolTip = LaunchSettingMetadataService.Tooltip(flag)
        };

        SetComboValue(combo, currentValue ?? (flag.Default?.ToString() ?? ""));
        return combo;
    }

    private static void SetComboValue(WpfComboBox combo, string value)
    {
        var match = combo.Items.Cast<object>().Select(item => item.ToString() ?? "").FirstOrDefault(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
        combo.SelectedItem = string.IsNullOrWhiteSpace(match) ? combo.Items[0] : match;
    }
}
