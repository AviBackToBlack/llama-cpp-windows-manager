using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;
using WpfBrush = System.Windows.Media.Brush;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace LocalLlmConsole;

/// <summary>Input parameters for creating the launch settings panel.</summary>
public sealed record LaunchSettingsPanelRequest(
    AppSettings Settings,
    IEnumerable<RuntimeChoice> RuntimeChoices,
    bool ShowAdvancedLaunchSettings,
    Action RuntimeSelectionChanged,
    Action<bool> AdvancedSettingsChanged,
    Action LaunchSettingsSearchChanged,
    Func<Task> SaveForModelAsync,
    Func<Task> SaveDefaultsAsync,
    Action ResetDefaults,
    Func<Task> SaveAsNewAsync,
    Func<Task> ChooseVisionProjectorAsync,
    Func<Task> ChooseMtpHeadAsync,
    Action SaveAsNewNameChanged);

/// <summary>Container for the controls and layout metadata produced by <see cref="LaunchSettingsPanelFactory"/>.</summary>
public sealed class LaunchSettingsPanelControls
{
    public required UIElement Root { get; init; }
    public required WpfComboBox RuntimeCombo { get; init; }
    public required TextBlock ModelCapabilityText { get; init; }
    public required WpfTextBox LaunchSettingsSearchBox { get; init; }
    public required WpfButton AdvancedLaunchSettingsButton { get; init; }
    public required WpfButton SaveModelLaunchSettingsButton { get; init; }
    public required WpfTextBox SaveAsNewModelNameBox { get; init; }
    public required WpfButton SaveAsNewModelButton { get; init; }
    public required LaunchSettingsFormControls FormControls { get; init; }
    public required Dictionary<string, List<FrameworkElement>> LaunchSettingElements { get; init; }
    public required HashSet<string> AdvancedLaunchSettingLabels { get; init; }
    public required List<LaunchSettingsSectionElements> LaunchSettingSections { get; init; }
    public required List<FrameworkElement> AdvancedLaunchSections { get; init; }
}

/// <summary>Describes a launch settings section and the labels it contains.</summary>
public sealed record LaunchSettingsSectionElements(
    string Title,
    FrameworkElement Section,
    IReadOnlyList<string> SettingLabels,
    bool IsAdvancedSection);

/// <summary>Factory that builds the launch settings panel and its control layout from the flag schema.</summary>
public static partial class LaunchSettingsPanelFactory
{
    public static LaunchSettingsPanelControls Create(LaunchSettingsPanelRequest request)
    {
        var launchSettingElements = new Dictionary<string, List<FrameworkElement>>(StringComparer.OrdinalIgnoreCase);
        var advancedLaunchSettingLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var launchSettingSections = new List<LaunchSettingsSectionElements>();
        var advancedLaunchSections = new List<FrameworkElement>();
        var panel = new StackPanel();

        var runtimeCombo = RuntimeCombo(request);
        var launchPortBox = LaunchTextBox(request.Settings.Port);
        launchPortBox.MinWidth = 78;
        launchPortBox.ToolTip = Loc.T("Tooltip.LaunchPortBox");
        panel.Children.Add(RuntimeAndPortRow(runtimeCombo, launchPortBox));

        var modelCapabilityText = Text(Loc.T("Launch.NoModelSelected"), 12, false, true);
        modelCapabilityText.TextWrapping = TextWrapping.NoWrap;
        modelCapabilityText.TextTrimming = TextTrimming.CharacterEllipsis;
        modelCapabilityText.Margin = new Thickness(0, 0, 0, 4);
        panel.Children.Add(modelCapabilityText);

        panel.Children.Add(LaunchSettingsToolbar(
            request,
            out var launchSettingsSearchBox,
            out var advancedLaunchSettingsButton));

        var builder = new LaunchSettingsPanelBuilder(
            launchSettingElements,
            advancedLaunchSettingLabels,
            launchSettingSections,
            advancedLaunchSections);
        var formControls = AddLaunchSections(panel, builder, request, launchPortBox);
        formControls.RuntimeCombo = runtimeCombo;

        panel.Children.Add(ActionButtons(request, out var saveForModelButton));
        panel.Children.Add(SaveAsNewRow(request, out var saveAsNewModelNameBox, out var saveAsNewModelButton));

        var root = new Border
        {
            Background = (WpfBrush)WpfApplication.Current.Resources["InputBack"],
            BorderBrush = (WpfBrush)WpfApplication.Current.Resources["PanelBorder"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(0),
            MinHeight = 220,
            Child = Scroll(panel, new Thickness(9, 8, 7, 8))
        };

        return new LaunchSettingsPanelControls
        {
            Root = root,
            RuntimeCombo = runtimeCombo,
            ModelCapabilityText = modelCapabilityText,
            LaunchSettingsSearchBox = launchSettingsSearchBox,
            AdvancedLaunchSettingsButton = advancedLaunchSettingsButton,
            SaveModelLaunchSettingsButton = saveForModelButton,
            SaveAsNewModelNameBox = saveAsNewModelNameBox,
            SaveAsNewModelButton = saveAsNewModelButton,
            FormControls = formControls,
            LaunchSettingElements = launchSettingElements,
            AdvancedLaunchSettingLabels = advancedLaunchSettingLabels,
            LaunchSettingSections = launchSettingSections,
            AdvancedLaunchSections = advancedLaunchSections
        };
    }
}
