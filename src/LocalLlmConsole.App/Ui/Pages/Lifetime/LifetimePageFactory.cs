using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace LocalLlmConsole;

public sealed record LifetimePageActions(
    RoutedEventHandler ResetLifetimeRowClick);

public sealed record LifetimePageRequest(
    IEnumerable Rows,
    LifetimePageActions Actions);

public sealed record LifetimePageControls(
    DataGrid MetricsGrid);

public sealed record LifetimePageBuildResult(
    DockPanel Content,
    LifetimePageControls Controls);

public static class LifetimePageFactory
{
    public static LifetimePageBuildResult Create(LifetimePageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Rows);
        ArgumentNullException.ThrowIfNull(request.Actions);

        var root = new DockPanel { Margin = new Thickness(16) };
        var metricsGrid = PageSectionFactory.GridFor(
            (Loc.T("Lifetime.Col.Model"), "C1", 2.4),
            (Loc.T("Lifetime.Col.Prompt"), "C2", .8),
            (Loc.T("Lifetime.Col.Generated"), "C3", .8),
            (Loc.T("Lifetime.Col.Total"), "C4", .8),
            (Loc.T("Lifetime.Col.Updated"), "C5", 1.1));
        PageSectionFactory.AddButtonColumn(metricsGrid, Loc.T("Lifetime.ResetButton"), "C6", "B1", request.Actions.ResetLifetimeRowClick, .55, tooltipBinding: "T1", visualRole: VisualRole.Danger);
        metricsGrid.ItemsSource = request.Rows;
        root.Children.Add(PageSectionFactory.GridSection(Loc.T("Lifetime.TokenUsageTitle"), metricsGrid));

        return new LifetimePageBuildResult(root, new LifetimePageControls(metricsGrid));
    }
}
