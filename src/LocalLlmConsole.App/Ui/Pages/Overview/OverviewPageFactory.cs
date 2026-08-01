using LocalLlmConsole.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;
using WpfBrush = System.Windows.Media.Brush;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfBinding = System.Windows.Data.Binding;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace LocalLlmConsole;

public sealed record OverviewPageActions(
    Func<Task> SelectModelSessionAsync,
    Func<Task> SelectLaunchProfileAsync,
    Func<Task> LoadSelectedModelAsync,
    Func<Task> SelectLoadedSessionRowAsync,
    RoutedEventHandler UnloadLoadedSessionRowClick);

public sealed record OverviewPageRequest(
    MainWindowViewModel ViewModel,
    OverviewPageActions Actions,
    Action<DataGrid> ConfigureRuntimeMetricsGrid);

public sealed record OverviewPageControls(
    Grid Root,
    WpfComboBox ModelCombo,
    WpfComboBox LaunchProfileCombo,
    WpfButton LoadButton,
    DataGrid LoadedSessionsGrid,
    Grid RuntimeDashboardModel,
    Grid RuntimeDashboardGpu,
    Grid RuntimeDashboardKvCache,
    Grid RuntimeDashboardTokens,
    TextBlock RuntimeDashboardTokensLastKnown,
    Grid RuntimeDashboardMtpTokens,
    Grid RuntimeDashboardSlots,
    MetricSparkline RuntimeDashboardTokensGraph,
    MetricSparkline RuntimeDashboardMtpTokensGraph,
    MetricSparkline RuntimeDashboardKvCacheGraph,
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

        var modelBar = ModelBar(request, out var modelCombo, out var launchProfileCombo, out var loadButton);
        Grid.SetRow(modelBar, 0);
        root.Children.Add(modelBar);

        var dashboardSection = Stack();
        var loadedSessionsGrid = PageSectionFactory.GridFor(
            (Loc.T("Overview.SessionsCol.Model"), "C1", 1.45),
            ("Profile", "C2", .9),
            (Loc.T("Overview.SessionsCol.Size"), "C3", .62),
            (Loc.T("Overview.SessionsCol.State"), "C4", .9),
            (Loc.T("Overview.SessionsCol.ApiEndpoints"), "C5", 1.9),
            (Loc.T("Overview.SessionsCol.Runtime"), "C6", 1.15),
            (Loc.T("Overview.SessionsCol.Backend"), "C7", .75));
        loadedSessionsGrid.ItemsSource = request.ViewModel.Overview.SessionRows;
        loadedSessionsGrid.SelectionChanged += async (_, _) => await request.Actions.SelectLoadedSessionRowAsync();
        PageSectionFactory.AddButtonColumn(
            loadedSessionsGrid,
            Loc.T("Common.ActionButton"),
            "C8",
            "B1",
            request.Actions.UnloadLoadedSessionRowClick,
            .58,
            tooltipProvider: _ => Loc.T("Tooltip.Unload"),
            visualRole: VisualRole.Danger);
        dashboardSection.Children.Add(PageSectionFactory.GridSection(Loc.T("Overview.LoadedSessionsTitle"), loadedSessionsGrid));
        dashboardSection.Children.Add(Text(Loc.T("Overview.ModelStatusLabel"), 18, true));

        var runtimeDashboard = RuntimeDashboard(
            out var runtimeDashboardModel,
            out var runtimeDashboardGpu,
            out var runtimeDashboardKvCache,
            out var runtimeDashboardTokens,
            out var runtimeDashboardTokensLastKnown,
            out var runtimeDashboardMtpTokens,
            out var runtimeDashboardSlots,
            out var runtimeDashboardTokensGraph,
            out var runtimeDashboardMtpTokensGraph,
            out var runtimeDashboardKvCacheGraph);
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
            launchProfileCombo,
            loadButton,
            loadedSessionsGrid,
            runtimeDashboardModel,
            runtimeDashboardGpu,
            runtimeDashboardKvCache,
            runtimeDashboardTokens,
            runtimeDashboardTokensLastKnown,
            runtimeDashboardMtpTokens,
            runtimeDashboardSlots,
            runtimeDashboardTokensGraph,
            runtimeDashboardMtpTokensGraph,
            runtimeDashboardKvCacheGraph,
            runtimeLogBox,
            runtimeMetricsGrid);
    }

    private static Grid ModelBar(
        OverviewPageRequest request,
        out WpfComboBox modelCombo,
        out WpfComboBox launchProfileCombo,
        out WpfButton loadButton)
    {
        var modelBar = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        modelBar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        modelBar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        modelBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        modelBar.ColumnDefinitions.Add(new ColumnDefinition());
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
            ItemTemplate = ModelNameTemplate(),
            SelectedValuePath = nameof(ModelRecord.Id),
            MinHeight = 30,
            Margin = new Thickness(0, 0, 8, 6),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            ToolTip = Loc.T("Tooltip.OverviewModelCombo")
        };
        modelCombo.SelectionChanged += async (_, _) => await request.Actions.SelectModelSessionAsync();
        Grid.SetColumn(modelCombo, 1);
        modelBar.Children.Add(modelCombo);

        var profileLabel = new TextBlock
        {
            Text = "Launch profile",
            FontWeight = FontWeights.SemiBold,
            Foreground = (WpfBrush)WpfApplication.Current.Resources["TextSoft"],
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 6)
        };
        Grid.SetRow(profileLabel, 1);
        modelBar.Children.Add(profileLabel);

        launchProfileCombo = new WpfComboBox
        {
            ItemsSource = request.ViewModel.Overview.LaunchProfileChoices,
            ItemTemplate = LaunchProfileNameTemplate(),
            SelectedValuePath = nameof(OverviewLaunchProfileChoice.Id),
            MinHeight = 30,
            Margin = new Thickness(0, 0, 8, 0),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            ToolTip = "Choose the named launch settings used when this model starts."
        };
        launchProfileCombo.SelectionChanged += async (_, _) => await request.Actions.SelectLaunchProfileAsync();
        Grid.SetRow(launchProfileCombo, 1);
        Grid.SetColumn(launchProfileCombo, 1);
        modelBar.Children.Add(launchProfileCombo);

        loadButton = Button(Loc.T("Overview.LoadButton"), request.Actions.LoadSelectedModelAsync, VisualRole.Primary);
        ConfigureLoadButton(loadButton);
        Grid.SetColumn(loadButton, 2);
        Grid.SetRow(loadButton, 1);
        modelBar.Children.Add(loadButton);

        return modelBar;
    }

    private static void ConfigureLoadButton(WpfButton button)
    {
        button.MinWidth = 94;
        button.MinHeight = 30;
        button.Margin = new Thickness(0);
        button.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
        button.VerticalAlignment = VerticalAlignment.Center;
    }

    private static DataTemplate ModelNameTemplate()
    {
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, new WpfBinding(nameof(ModelRecord.Name)));
        text.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        text.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        return new DataTemplate(typeof(ModelRecord)) { VisualTree = text };
    }

    private static DataTemplate LaunchProfileNameTemplate()
    {
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, new WpfBinding(nameof(OverviewLaunchProfileChoice.Name)));
        text.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        text.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        return new DataTemplate(typeof(OverviewLaunchProfileChoice)) { VisualTree = text };
    }

    private static Grid RuntimeDashboard(
        out Grid model,
        out Grid gpu,
        out Grid kvCache,
        out Grid tokens,
        out TextBlock tokensLastKnown,
        out Grid mtpTokens,
        out Grid slots,
        out MetricSparkline tokensGraph,
        out MetricSparkline mtpTokensGraph,
        out MetricSparkline kvCacheGraph)
    {
        var runtimeDashboard = new Grid { Margin = new Thickness(0, 2, 0, 8) };

        model = MetricCardFactory.AddMetric(runtimeDashboard, Loc.T("Overview.Metric.ModelStatus"), 0, 0, labelKey: "Overview.Metric.ModelStatus");
        gpu = MetricCardFactory.AddMetric(runtimeDashboard, Loc.T("Overview.Metric.Hardware"), 0, 1);
        slots = MetricCardFactory.AddMetric(runtimeDashboard, Loc.T("Overview.Metric.Slots"), 0, 2);
        tokens = MetricCardFactory.AddMetricGraph(runtimeDashboard, Loc.T("Overview.Metric.Tokens"), 1, 0, out tokensGraph, out tokensLastKnown);
        mtpTokens = MetricCardFactory.AddMetricGraph(
            runtimeDashboard,
            Loc.T("Overview.Metric.MtpTokens"),
            1,
            1,
            out mtpTokensGraph,
            out _,
            primaryBrushKey: "Warning",
            secondaryBrushKey: "Accent");
        kvCache = MetricCardFactory.AddMetricGraph(
            runtimeDashboard,
            Loc.T("Overview.Metric.KvCache"),
            1,
            2,
            out kvCacheGraph,
            out _,
            fixedMaximum: 100);
        ConfigureResponsiveMetricLayout(runtimeDashboard);
        return runtimeDashboard;
    }

    private static void ConfigureResponsiveMetricLayout(Grid dashboard)
    {
        var cards = dashboard.Children.OfType<Border>().ToArray();
        var appliedColumns = 0;

        void Apply(double availableWidth)
        {
            var columns = availableWidth >= 930 ? 3 : availableWidth >= 620 ? 2 : 1;
            if (columns == appliedColumns) return;
            appliedColumns = columns;

            dashboard.ColumnDefinitions.Clear();
            dashboard.RowDefinitions.Clear();
            for (var column = 0; column < columns; column++)
                dashboard.ColumnDefinitions.Add(new ColumnDefinition());
            var rows = (int)Math.Ceiling(cards.Length / (double)columns);
            for (var row = 0; row < rows; row++)
                dashboard.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            for (var index = 0; index < cards.Length; index++)
            {
                var column = index % columns;
                Grid.SetRow(cards[index], index / columns);
                Grid.SetColumn(cards[index], column);
                cards[index].Margin = new Thickness(column == 0 ? 0 : 5, 0, column == columns - 1 ? 0 : 5, 8);
            }
        }

        dashboard.Loaded += (_, _) => Apply(dashboard.ActualWidth);
        dashboard.SizeChanged += (_, args) => Apply(args.NewSize.Width);
        Apply(0);
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

    private static WpfButton Button(string text, Func<Task> click, string visualRole = "")
    {
        var button = new WpfButton { Content = text };
        VisualRole.SetButtonRole(button, visualRole);
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
