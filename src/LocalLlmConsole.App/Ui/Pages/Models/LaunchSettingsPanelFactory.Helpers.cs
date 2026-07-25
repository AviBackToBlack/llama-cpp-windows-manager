using System.Windows;
using System.Windows.Controls;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace LocalLlmConsole;

public static partial class LaunchSettingsPanelFactory
{
    private static WpfTextBox AddFirstClassTextBox(
        Grid grid,
        LaunchSettingsPanelBuilder builder,
        string label,
        string flagName,
        int value,
        HashSet<string> excludedFlags,
        bool advanced = false)
        => AddFirstClassTextBox(grid, builder, label, flagName, value.ToString(System.Globalization.CultureInfo.InvariantCulture), excludedFlags, advanced);

    private static WpfTextBox AddFirstClassTextBox(
        Grid grid,
        LaunchSettingsPanelBuilder builder,
        string label,
        string flagName,
        double value,
        HashSet<string> excludedFlags,
        bool advanced = false)
        => AddFirstClassTextBox(grid, builder, label, flagName, value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), excludedFlags, advanced);

    private static WpfTextBox AddFirstClassTextBox(
        Grid grid,
        LaunchSettingsPanelBuilder builder,
        string label,
        string flagName,
        string value,
        HashSet<string> excludedFlags,
        bool advanced = false)
    {
        var box = LaunchTextBox(value);
        AddFirstClassControl(grid, builder, label, flagName, box, excludedFlags, advanced);
        return box;
    }

    private static WpfComboBox AddFirstClassCombo(
        Grid grid,
        LaunchSettingsPanelBuilder builder,
        string label,
        string flagName,
        string value,
        IEnumerable<string> options,
        HashSet<string> excludedFlags,
        bool advanced = false)
    {
        var combo = LaunchCombo(options);
        SetComboValue(combo, value);
        AddFirstClassControl(grid, builder, label, flagName, combo, excludedFlags, advanced);
        return combo;
    }

    private static void AddFirstClassControl(
        Grid grid,
        LaunchSettingsPanelBuilder builder,
        string label,
        string flagName,
        FrameworkElement control,
        HashSet<string> excludedFlags,
        bool advanced = false)
    {
        control.Tag = flagName;
        control.ToolTip = LaunchSettingMetadataService.Tooltip(label, flagName);
        if (advanced)
            builder.AddAdvancedLaunchSetting(grid, label, control);
        else
            builder.AddLaunchSetting(grid, label, control);
        // AddGeneratedFlags excludes by schema PrimaryName, so a control registered under an
        // alias (e.g. --model-draft for --spec-draft-model) must exclude every alias or the
        // generated pass creates a duplicate control for the same flag.
        excludedFlags.Add(flagName);
        var schemaFlag = LlamaServerFlagSchema.FindByName(flagName);
        if (schemaFlag is not null)
        {
            foreach (var name in schemaFlag.Names)
                excludedFlags.Add(name);
        }
    }

    private static void SetComboValue(WpfComboBox combo, string value)
        => LaunchSettingsControlFactory.SetComboValue(combo, value, fallbackToFirstItem: true);
}
