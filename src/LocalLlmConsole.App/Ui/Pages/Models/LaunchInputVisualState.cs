using System.Windows;

namespace LocalLlmConsole;

public enum LaunchInputState
{
    Default,
    Valid,
    Invalid
}

/// <summary>Exposes validation state to WPF control templates without repurposing control tags.</summary>
public static class LaunchInputVisualState
{
    public static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
        "State",
        typeof(LaunchInputState),
        typeof(LaunchInputVisualState),
        new FrameworkPropertyMetadata(LaunchInputState.Default));

    public static LaunchInputState GetState(DependencyObject element)
        => (LaunchInputState)element.GetValue(StateProperty);

    public static void SetState(DependencyObject element, LaunchInputState value)
        => element.SetValue(StateProperty, value);
}
