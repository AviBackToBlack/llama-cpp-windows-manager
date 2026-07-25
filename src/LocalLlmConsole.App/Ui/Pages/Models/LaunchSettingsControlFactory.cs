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
        Text = currentValue ?? "",
        MinHeight = 29,
        MinWidth = 72,
        Margin = new Thickness(0, 0, 4, 2),
        HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
        ToolTip = LaunchSettingMetadataService.Tooltip(flag)
    };

    private static WpfComboBox CreateComboBox(LlamaServerFlag flag, string? currentValue)
    {
        var values = flag.ValueType == FlagValueType.Boolean
            ? BooleanComboOptions(flag)
            : (flag.AllowedValues ?? Array.Empty<string>());

        var combo = new WpfComboBox
        {
            ItemsSource = values.ToArray(),
            MinHeight = 27,
            MinWidth = 76,
            Margin = new Thickness(0, 0, 6, 4),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            ToolTip = LaunchSettingMetadataService.Tooltip(flag)
        };

        SetComboValue(combo, currentValue ?? "");
        return combo;
    }

    private static IEnumerable<string> BooleanComboOptions(LlamaServerFlag flag)
    {
        if (flag.AllowedValues is not null) return flag.AllowedValues;
        // Boolean pairs are represented as a single tri-state control; a negated form
        // means the user can explicitly turn the feature on or off.
        return flag.NegatedForm is not null
            ? new[] { "auto", "on", "off" }
            : new[] { "on", "off", "auto" };
    }

    /// <summary>
    /// Selects the combo item matching <paramref name="value"/>, translating the parser's
    /// boolean and speculative-type spellings into the on/off/auto vocabulary the combos use.
    /// This is the single normalization used by every launch-settings combo; the panel factory
    /// and form binder delegate here so the accepted spellings cannot drift apart.
    /// </summary>
    /// <param name="fallbackToFirstItem">
    /// Selects the first item when no item matches. Panel construction and settings-to-form
    /// binding use this so a combo always shows a value; command-line round-tripping does not,
    /// because an unmatched value there means the user typed something the combo cannot hold.
    /// </param>
    public static void SetComboValue(WpfComboBox combo, string? value, bool fallbackToFirstItem = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            combo.SelectedItem = null;
            return;
        }

        var normalized = NormalizeComboValue(value);
        var match = combo.Items.Cast<object>().Select(item => item.ToString() ?? "").FirstOrDefault(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(match))
        {
            combo.SelectedItem = match;
            return;
        }

        if (fallbackToFirstItem && combo.Items.Count > 0)
        {
            combo.SelectedItem = combo.Items[0];
            System.Diagnostics.Debug.WriteLine($"Warning: could not match combo value '{value}'; falling back to '{combo.Items[0]}'.");
            return;
        }

        combo.SelectedItem = null;
        System.Diagnostics.Debug.WriteLine($"Warning: could not match combo value '{value}'; leaving the combo unselected.");
    }

    public static string NormalizeComboValue(string value)
    {
        var normalized = value.Trim();
        if (string.Equals(normalized, "true", StringComparison.OrdinalIgnoreCase)) return "on";
        if (string.Equals(normalized, "false", StringComparison.OrdinalIgnoreCase)) return "off";
        if (string.Equals(normalized, "mtp", StringComparison.OrdinalIgnoreCase)) return LaunchSettingMetadataService.AtomicMtpSpeculativeType;
        return normalized;
    }
}
