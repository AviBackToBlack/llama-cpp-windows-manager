using LocalLlmConsole.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;
using WpfBrush = System.Windows.Media.Brush;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace LocalLlmConsole;

public sealed record OverviewPageActions(
    Func<Task> SelectModelSessionAsync,
    Func<Task> LoadSelectedModelAsync,
    Func<Task> UnloadSelectedModelAsync,
    Func<Task> SelectLoadedSessionRowAsync);

public sealed record OverviewPageRequest(
    MainWindowViewModel ViewModel,
    OverviewPageActions Actions,
    Action<DataGrid> ConfigureRuntimeMetricsGrid);

public sealed record OverviewPageControls(
    Grid Root,
    WpfComboBox ModelCombo,
    WpfButton LoadButton,
    WpfButton UnloadButton,
    DataGrid LoadedSessionsGrid,
    Grid RuntimeDashboardModel,
    Grid RuntimeDashboardGpu,
    Grid RuntimeDashboardRequests,
    Grid RuntimeDashboardTokens,
    TextBlock RuntimeDashboardTokensLastKnown,
    Grid RuntimeDashboardMtpTokens,
    Grid RuntimeDashboardSlots,
    WpfTextBox RuntimeLogBox,
    DataGrid RuntimeMetricsGrid);

public static class OverviewPageFactory
{


    public static OverviewPageControls Create(OverviewPageRequest request)
    {
        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.08, GridUnitType.Star), MinHeight = 150 });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(.92, GridUnitType.Star), MinHeight = 130 });

        var modelBar = ModelBar(request, out var modelCombo, out var loadButton, out var unloadButton);
        Grid.SetRow(modelBar, 0);
        root.Children.Add(modelBar);

        var dashboardSection = Stack();
        var loadedSessionsGrid = PageSectionFactory.GridFor(
            (Loc.T("Overview.SessionsCol.Model"), "C1", 1.45),
            (Loc.T("Overview.SessionsCol.Size"), "C2", .62),
            (Loc.T("Overview.SessionsCol.State"), "C3", .62),
            (Loc.T("Overview.SessionsCol.ApiEndpoints"), "C4", 1.9),
            (Loc.T("Overview.SessionsCol.Runtime"), "C5", 1.25),
            (Loc.T("Overview.SessionsCol.Backend"), "C6", .75));
        loadedSessionsGrid.ItemsSource = request.ViewModel.Overview.SessionRows;
        loadedSessionsGrid.SelectionChanged += async (_, _) => await request.Actions.SelectLoadedSessionRowAsync();
        dashboardSection.Children.Add(PageSectionFactory.GridSection(Loc.T("Overview.LoadedSessionsTitle"), loadedSessionsGrid));
        dashboardSection.Children.Add(Text(Loc.T("Overview.ModelStatusLabel"), 18, true));

        var runtimeDashboard = RuntimeDashboard(
            out var runtimeDashboardModel,
            out var runtimeDashboardGpu,
            out var runtimeDashboardRequests,
            out var runtimeDashboardTokens,
            out var runtimeDashboardTokensLastKnown,
            out var runtimeDashboardMtpTokens,
            out var runtimeDashboardSlots);
        dashboardSection.Children.Add(runtimeDashboard);
        Grid.SetRow(dashboardSection, 1);
        root.Children.Add(dashboardSection);

        var runtimeLogBox = RuntimeLogBox();
        var runtimeLogSection = PageSectionFactory.FramedSection(Loc.T("Overview.LiveRuntimeLogTitle"), runtimeLogBox);
        Grid.SetRow(runtimeLogSection, 2);
        root.Children.Add(runtimeLogSection);
        root.Children.Add(PageSectionFactory.HorizontalGridSplitter(3));

        var runtimeMetricsGrid = PageSectionFactory.GridFor(
            (Loc.T("Overview.MetricsCol.Metric"), "C1", 1.5),
            (Loc.T("Overview.MetricsCol.Labels"), "C2", 2.2),
            (Loc.T("Overview.MetricsCol.Value"), "C3", .9),
            (Loc.T("Overview.MetricsCol.Type"), "C4", .7),
            (Loc.T("Overview.MetricsCol.Help"), "C5", 3));
        runtimeMetricsGrid.ItemsSource = request.ViewModel.RuntimeMetrics.Rows;
        runtimeMetricsGrid.VerticalAlignment = VerticalAlignment.Stretch;
        request.ConfigureRuntimeMetricsGrid(runtimeMetricsGrid);
        var metricsSection = PageSectionFactory.GridSection(Loc.T("Overview.RuntimeMetricsTitle"), runtimeMetricsGrid);
        Grid.SetRow(metricsSection, 4);
        root.Children.Add(metricsSection);

        return new OverviewPageControls(
            root,
            modelCombo,
            loadButton,
            unloadButton,
            loadedSessionsGrid,
            runtimeDashboardModel,
            runtimeDashboardGpu,
            runtimeDashboardRequests,
            runtimeDashboardTokens,
            runtimeDashboardTokensLastKnown,
            runtimeDashboardMtpTokens,
            runtimeDashboardSlots,
            runtimeLogBox,
            runtimeMetricsGrid);
    }

    private static Grid ModelBar(OverviewPageRequest request, out WpfComboBox modelCombo, out WpfButton loadButton, out WpfButton unloadButton)
    {
        var modelBar = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        modelBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        modelBar.ColumnDefinitions.Add(new ColumnDefinition());
        modelBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        modelBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        modelBar.Children.Add(new TextBlock
        {
            Text = Loc.T("Overview.ModelLabel"),
            FontWeight = FontWeights.SemiBold,
            Foreground = (WpfBrush)WpfApplication.Current.Resources["TextSoft"],
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 6)
        });
        modelCombo = new WpfComboBox
        {
            ItemsSource = request.ViewModel.Overview.ModelChoices,
            DisplayMemberPath = nameof(ModelRecord.Name),
            SelectedValuePath = nameof(ModelRecord.Id),
            MinHeight = 30,
            Margin = new Thickness(0, 0, 8, 6),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            ToolTip = Loc.T("Tooltip.OverviewModelCombo")
        };
        modelCombo.SelectionChanged += async (_, _) => await request.Actions.SelectModelSessionAsync();
        Grid.SetColumn(modelCombo, 1);
        modelBar.Children.Add(modelCombo);

        loadButton = Button(Loc.T("Overview.LoadButton"), request.Actions.LoadSelectedModelAsync);
        Grid.SetColumn(loadButton, 2);
        modelBar.Children.Add(loadButton);

        unloadButton = Button(Loc.T("Overview.UnloadButton"), request.Actions.UnloadSelectedModelAsync);
        Grid.SetColumn(unloadButton, 3);
        modelBar.Children.Add(unloadButton);
        return modelBar;
    }

    private static Grid RuntimeDashboard(
        out Grid model,
        out Grid gpu,
        out Grid requests,
        out Grid tokens,
        out TextBlock tokensLastKnown,
        out Grid mtpTokens,
        out Grid slots)
    {
        var runtimeDashboard = new Grid { Margin = new Thickness(0, 2, 0, 8) };
        runtimeDashboard.ColumnDefinitions.Add(new ColumnDefinition());
        runtimeDashboard.ColumnDefinitions.Add(new ColumnDefinition());
        runtimeDashboard.ColumnDefinitions.Add(new ColumnDefinition());
        for (var row = 0; row < 2; row++)
            runtimeDashboard.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        model = MetricCardFactory.AddMetric(runtimeDashboard, Loc.T("Overview.Metric.ModelStatus"), 0, 0, labelKey: "Overview.Metric.ModelStatus");
        gpu = MetricCardFactory.AddMetric(runtimeDashboard, Loc.T("Overview.Metric.Hardware"), 0, 1);
        requests = MetricCardFactory.AddMetric(runtimeDashboard, Loc.T("Overview.Metric.Settings"), 0, 2);
        tokens = MetricCardFactory.AddMetric(runtimeDashboard, Loc.T("Overview.Metric.Tokens"), 1, 0, out tokensLastKnown);
        mtpTokens = MetricCardFactory.AddMetric(runtimeDashboard, Loc.T("Overview.Metric.MtpTokens"), 1, 1);
        slots = MetricCardFactory.AddMetric(runtimeDashboard, Loc.T("Overview.Metric.Slots"), 1, 2);
        return runtimeDashboard;
    }

    private static WpfTextBox RuntimeLogBox()
        => new()
        {
            IsReadOnly = true,
            Text = Loc.T("Overview.NoRuntimeLog"),
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0),
            TextWrapping = TextWrapping.NoWrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

    private static WpfButton Button(string text, Func<Task> click)
    {
        var button = new WpfButton { Content = text };
        button.ToolTip = string.Equals(text, Loc.T("Overview.LoadButton")) ? Loc.T("Tooltip.Load")
            : string.Equals(text, Loc.T("Overview.UnloadButton")) ? Loc.T("Tooltip.Unload")
            : Loc.T("Common.RunAction", text);
        ToolTipService.SetShowOnDisabled(button, true);
        button.Click += async (_, _) => await click();
        return button;
    }

    private static StackPanel Stack() => new();

    private static TextBlock Text(string text, int size = 13, bool bold = false, bool muted = false) => new()
    {
        Text = text,
        FontSize = size,
        FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
        Foreground = muted ? (WpfBrush)WpfApplication.Current.Resources["TextMuted"] : (WpfBrush)WpfApplication.Current.Resources["TextMain"],
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, size >= 18 ? 10 : 0, 0, size >= 18 ? 10 : 8)
    };
}
