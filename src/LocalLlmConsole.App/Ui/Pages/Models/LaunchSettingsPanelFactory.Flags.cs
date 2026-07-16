using System.Windows;
using System.Windows.Controls;
using LocalLlmConsole.Services;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace LocalLlmConsole;

public static partial class LaunchSettingsPanelFactory
{
    private static void AddGeneratedSection(
        StackPanel panel,
        LaunchSettingsPanelBuilder builder,
        string category,
        HashSet<string> excludedFlags,
        Dictionary<string, FrameworkElement> generatedControls,
        LaunchSettingsFormControls formControls)
    {
        var grid = LaunchSettingsGrid();
        AddGeneratedFlags(grid, builder, category, excludedFlags, generatedControls, formControls);
        if (grid.Children.Count > 0)
            AddLaunchSection(panel, builder, category, grid, isAdvancedSection: true);
    }

    private static void AddGeneratedFlags(
        Grid grid,
        LaunchSettingsPanelBuilder builder,
        string category,
        HashSet<string> excludedFlags,
        Dictionary<string, FrameworkElement> generatedControls,
        LaunchSettingsFormControls formControls,
        Func<LlamaServerFlag, bool>? include = null,
        Func<LlamaServerFlag, bool>? skip = null)
    {
        var flags = LlamaServerFlagSchema.All.Where(f =>
            string.Equals(f.Category, category, StringComparison.OrdinalIgnoreCase) &&
            !excludedFlags.Contains(f.PrimaryName) &&
            (include is null || include(f)) &&
            (skip is null || !skip(f))).ToList();

        foreach (var flag in flags)
        {
            var value = GetGeneratedFlagValue(formControls, flag);
            var control = LaunchSettingsControlFactory.CreateControl(flag, value);
            var label = flag.UiLabel;
            var advanced = !BasicFlagNames.Contains(flag.PrimaryName, StringComparer.OrdinalIgnoreCase);
            if (advanced)
                builder.AddAdvancedLaunchSetting(grid, label, control);
            else
                builder.AddLaunchSetting(grid, label, control);
            generatedControls[flag.PrimaryName] = control;
            excludedFlags.Add(flag.PrimaryName);
        }
    }

    private static string? GetGeneratedFlagValue(LaunchSettingsFormControls formControls, LlamaServerFlag flag)
    {
        var key = flag.PrimaryName;
        if (key.StartsWith("--no-", StringComparison.OrdinalIgnoreCase))
            key = "--" + key[5..];
        if (formControls.TryGetValueByFlagName(key, out var value))
            return value;
        if (flag.Default is null) return null;
        return LaunchSettingsFormControls.NormalizeDefaultValue(flag.Default);
    }

    private static bool IsContextExtensionFlag(LlamaServerFlag flag)
        => flag.PrimaryName.Contains("rope", StringComparison.OrdinalIgnoreCase) ||
           flag.PrimaryName.Contains("yarn", StringComparison.OrdinalIgnoreCase);

    private static bool IsChatCapabilityFlag(LlamaServerFlag flag)
        => flag.PrimaryName.Contains("reason", StringComparison.OrdinalIgnoreCase) ||
           flag.PrimaryName.Contains("jinja", StringComparison.OrdinalIgnoreCase) ||
           flag.PrimaryName.Contains("mmproj", StringComparison.OrdinalIgnoreCase) ||
           flag.PrimaryName.Contains("vision", StringComparison.OrdinalIgnoreCase) ||
           flag.PrimaryName.Contains("image", StringComparison.OrdinalIgnoreCase) ||
           flag.PrimaryName.Contains("chat-template", StringComparison.OrdinalIgnoreCase) ||
           flag.PrimaryName.Contains("prefill", StringComparison.OrdinalIgnoreCase) ||
           flag.PrimaryName.Contains("skip-chat-parsing", StringComparison.OrdinalIgnoreCase);
}
