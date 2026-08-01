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
    private static void ApplyTheme(string mode)
    {
        var dark = AppPreferenceService.ThemeMode(mode) switch
        {
            "light" => false,
            "dark" => true,
            _ => IsSystemDarkTheme()
        };

        foreach (var (key, color) in dark ? DarkThemeColors() : LightThemeColors())
        {
            var resolved = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color);
            if (WpfApplication.Current.Resources[key] is SolidColorBrush brush && !brush.IsFrozen)
            {
                brush.Color = resolved;
            }
            else
            {
                WpfApplication.Current.Resources[key] = new SolidColorBrush(resolved);
            }
        }
    }

    private static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return true;
        }
    }

    private static (string Key, string Color)[] DarkThemeColors() =>
    [
        ("AppBack", "#090E16"),
        ("SidebarBack", "#101927"),
        ("PanelBack", "#121C2A"),
        ("PanelBackAlt", "#192638"),
        ("SurfaceRaised", "#1C2B3E"),
        ("SectionHeaderBack", "#172436"),
        ("PanelBorder", "#293A4F"),
        ("PanelBorderStrong", "#3B516B"),
        ("ControlBack", "#1D2C3F"),
        ("ControlHover", "#273B53"),
        ("ControlPressed", "#142235"),
        ("InputBack", "#0D1623"),
        ("ReadOnlyBack", "#111C2A"),
        ("GridRowBack", "#111B29"),
        ("GridRowAlt", "#152234"),
        ("TextMain", "#F3F7FB"),
        ("TextMuted", "#96A9BC"),
        ("TextSoft", "#CBD7E3"),
        ("Accent", "#4DE0BE"),
        ("AccentStrong", "#27C99D"),
        ("AccentHover", "#62E8C8"),
        ("AccentForeground", "#061812"),
        ("AccentSoft", "#163B38"),
        ("AccentBlue", "#6CB6FF"),
        ("InfoSoft", "#183451"),
        ("SelectionBack", "#185047"),
        ("FocusRing", "#77E8CF"),
        ("Success", "#55D98C"),
        ("SuccessSoft", "#173D2B"),
        ("Warning", "#F6C45B"),
        ("WarningSoft", "#483719"),
        ("Danger", "#FF7A84"),
        ("DangerHover", "#FF97A0"),
        ("DangerSoft", "#47242C"),
        ("StatusBack", "#0F2130"),
        ("ShadowColor", "#02050A"),
        ("StatusQueued", "#29334A"),
        ("StatusRunning", "#174737"),
        ("StatusFailed", "#512832"),
        ("StatusCancelled", "#3C2B36")
    ];

    private static (string Key, string Color)[] LightThemeColors() =>
    [
        ("AppBack", "#E9EFF5"),
        ("SidebarBack", "#F4F8FC"),
        ("PanelBack", "#FFFFFF"),
        ("PanelBackAlt", "#F3F7FB"),
        ("SurfaceRaised", "#F8FBFE"),
        ("SectionHeaderBack", "#EAF1F7"),
        ("PanelBorder", "#C7D3DF"),
        ("PanelBorderStrong", "#91A5B8"),
        ("ControlBack", "#F4F8FC"),
        ("ControlHover", "#E2ECF5"),
        ("ControlPressed", "#CDDCE9"),
        ("InputBack", "#FFFFFF"),
        ("ReadOnlyBack", "#EDF3F8"),
        ("GridRowBack", "#FFFFFF"),
        ("GridRowAlt", "#F2F7FB"),
        ("TextMain", "#142131"),
        ("TextMuted", "#586A7C"),
        ("TextSoft", "#2C4156"),
        ("Accent", "#087A64"),
        ("AccentStrong", "#0B8C71"),
        ("AccentHover", "#0AA987"),
        ("AccentForeground", "#FFFFFF"),
        ("AccentSoft", "#D7F1EA"),
        ("AccentBlue", "#1769AA"),
        ("InfoSoft", "#DDECF8"),
        ("SelectionBack", "#C7EADF"),
        ("FocusRing", "#087A64"),
        ("Success", "#187A45"),
        ("SuccessSoft", "#DDF3E6"),
        ("Warning", "#8A5100"),
        ("WarningSoft", "#F8EACB"),
        ("Danger", "#B42335"),
        ("DangerHover", "#D13245"),
        ("DangerSoft", "#FAE1E5"),
        ("StatusBack", "#E7F2F8"),
        ("ShadowColor", "#60758A"),
        ("StatusQueued", "#E2E8F2"),
        ("StatusRunning", "#D7EFE4"),
        ("StatusFailed", "#F8DFE4"),
        ("StatusCancelled", "#ECE3EA")
    ];
}
