using System.Windows;

namespace LocalLlmConsole;

public static class VisualRole
{
    public const string Primary = "Primary";
    public const string Danger = "Danger";
    public const string Quiet = "Quiet";

    public static readonly DependencyProperty ButtonRoleProperty = DependencyProperty.RegisterAttached(
        "ButtonRole",
        typeof(string),
        typeof(VisualRole),
        new FrameworkPropertyMetadata(""));

    public static string GetButtonRole(DependencyObject element)
        => (string)element.GetValue(ButtonRoleProperty);

    public static void SetButtonRole(DependencyObject element, string value)
        => element.SetValue(ButtonRoleProperty, value);
}
