using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;
using WpfBrush = System.Windows.Media.Brush;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;

namespace LocalLlmConsole;

public sealed record RuntimesPageActions(
    Func<Task> ChooseRuntimeFolderAsync,
    Func<Task> ChangeCudaPackagePreferenceAsync,
    Action ToggleAdvancedRuntimes,
    MouseButtonEventHandler RuntimeGridPreviewMouseLeftButtonDown,
    RoutedEventHandler BuildRuntimeRowClick,
    RoutedEventHandler DeleteRuntimeRowClick,
    RoutedEventHandler InstallRuntimePackageRowClick,
    RoutedEventHandler CheckRuntimePackageUpdateRowClick,
    RoutedEventHandler DeleteRuntimePackageRowClick,
    RoutedEventHandler DownloadRuntimePresetRowClick,
    RoutedEventHandler CheckRuntimePresetUpdateRowClick,
    RoutedEventHandler DeleteRuntimePresetRowClick,
    RoutedEventHandler OpenRuntimeJobLogRowClick,
    RoutedEventHandler CancelRuntimeJobRowClick,
    RoutedEventHandler RetryRuntimeJobRowClick,
    RoutedEventHandler ClearRuntimeJobRowClick,
    Action<DataGrid> ConfigureRuntimeGridColumnSizing,
    Action<DataGrid> ConfigureRuntimeBuildGridColumnSizing,
    Action<DataGrid> ConfigureRuntimeJobsGridColumnSizing);

public sealed record RuntimesPageRequest(
    MainWindowViewModel ViewModel,
    string RuntimeRoot,
    bool ShowAdvancedRuntimes,
    string CudaPackagePreference,
    RuntimesPageActions Actions);

public sealed record RuntimesPageControls(
    Grid Root,
    TextBlock RuntimesFolderText,
    DataGrid RuntimeGrid,
    DataGrid RuntimePackageGrid,
    DataGrid? RuntimeBuildGrid,
    DataGrid? RuntimeJobsGrid,
    WpfButton RuntimeAdvancedToggleButton,
    WpfComboBox RuntimeCudaPreferenceCombo);

public static class RuntimesPageFactory
{
    public static RuntimesPageControls Create(RuntimesPageRequest request)
    {
        var root = RootGrid(request.ShowAdvancedRuntimes);
        var (header, runtimesFolderText, runtimeAdvancedToggleButton, runtimeCudaPreferenceCombo) = Header(request);
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var runtimeGrid = InstalledRuntimesGrid(request);
        var runtimeSection = PageSectionFactory.GridSection(
            Loc.T("Runtimes.InstalledLocalBuildsTitle"),
            runtimeGrid,
            Loc.T("Runtimes.InstalledLocalBuildsDesc"));
        Grid.SetRow(runtimeSection, 1);
        root.Children.Add(runtimeSection);
        root.Children.Add(PageSectionFactory.HorizontalGridSplitter(2));

        var runtimePackageGrid = RuntimePackageGrid(request);
        var packageSection = PageSectionFactory.GridSection(
            Loc.T("Runtimes.RuntimeDownloadsTitle"),
            runtimePackageGrid,
            Loc.T("Runtimes.RuntimeDownloadsDesc"));
        Grid.SetRow(packageSection, 3);
        root.Children.Add(packageSection);
        if (request.ShowAdvancedRuntimes)
            root.Children.Add(PageSectionFactory.HorizontalGridSplitter(4));

        var runtimeBuildGrid = request.ShowAdvancedRuntimes ? RuntimeBuildGrid(request) : null;
        var runtimeJobsGrid = request.ShowAdvancedRuntimes ? RuntimeJobsGrid(request) : null;
        if (request.ShowAdvancedRuntimes)
        {
            var buildSection = PageSectionFactory.GridSection(
                Loc.T("Runtimes.BuildFromSourceTitle"),
                runtimeBuildGrid!,
                Loc.T("Runtimes.BuildFromSourceDesc"));
            Grid.SetRow(buildSection, 5);
            root.Children.Add(buildSection);
            root.Children.Add(PageSectionFactory.HorizontalGridSplitter(6));

            var jobsSection = PageSectionFactory.GridSection(
                Loc.T("Runtimes.RuntimeJobsTitle"),
                runtimeJobsGrid!,
                Loc.T("Runtimes.RuntimeJobsDesc"));
            Grid.SetRow(jobsSection, 7);
            root.Children.Add(jobsSection);
        }

        return new RuntimesPageControls(
            root,
            runtimesFolderText,
            runtimeGrid,
            runtimePackageGrid,
            runtimeBuildGrid,
            runtimeJobsGrid,
            runtimeAdvancedToggleButton,
            runtimeCudaPreferenceCombo);
    }

    private static Grid RootGrid(bool showAdvancedRuntimes)
    {
        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(showAdvancedRuntimes ? .72 : 1, GridUnitType.Star), MinHeight = 86 });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(showAdvancedRuntimes ? .72 : 1, GridUnitType.Star), MinHeight = 94 });
        if (showAdvancedRuntimes)
        {
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(.72, GridUnitType.Star), MinHeight = 94 });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(.82, GridUnitType.Star), MinHeight = 120 });
        }

        return root;
    }

    private static (Grid Header, TextBlock RuntimesFolderText, WpfButton AdvancedToggle, WpfComboBox CudaPreferenceCombo) Header(RuntimesPageRequest request)
    {
        var folderStrip = FolderStripActionsFirst(
            Loc.T("Runtimes.FolderLabel"),
            request.RuntimeRoot,
            out var runtimesFolderText,
            (Loc.T("Runtimes.ChooseFolderButton"), request.Actions.ChooseRuntimeFolderAsync));
        var header = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        folderStrip.Margin = new Thickness(0);
        header.Children.Add(folderStrip);
        var rightActions = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        rightActions.Children.Add(new TextBlock
        {
            Text = Loc.T("Runtimes.CudaDownloadsLabel"),
            Foreground = (WpfBrush)WpfApplication.Current.Resources["TextMuted"],
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 8, 6)
        });
        var runtimeCudaPreferenceCombo = LaunchCombo(AppPreferenceService.CudaPackagePreferenceOptions());
        runtimeCudaPreferenceCombo.Width = 132;
        runtimeCudaPreferenceCombo.SelectedItem = AppPreferenceService.CudaPackagePreferenceLabel(request.CudaPackagePreference);
        runtimeCudaPreferenceCombo.ToolTip = Loc.T("Tooltip.CudaPreferenceCombo");
        runtimeCudaPreferenceCombo.SelectionChanged += async (_, _) => await request.Actions.ChangeCudaPackagePreferenceAsync();
        rightActions.Children.Add(runtimeCudaPreferenceCombo);
        var runtimeAdvancedToggleButton = Button(request.ShowAdvancedRuntimes ? Loc.T("Runtimes.HideAdvancedButton") : Loc.T("Runtimes.ShowAdvancedButton"), () =>
        {
            request.Actions.ToggleAdvancedRuntimes();
            return Task.CompletedTask;
        });
        runtimeAdvancedToggleButton.ToolTip = request.ShowAdvancedRuntimes ? Loc.T("Tooltip.RuntimesHideAdvanced") : Loc.T("Tooltip.RuntimesShowAdvanced");
        ToolTipService.SetShowOnDisabled(runtimeAdvancedToggleButton, true);
        runtimeAdvancedToggleButton.Margin = new Thickness(12, 0, 0, 6);
        rightActions.Children.Add(runtimeAdvancedToggleButton);
        Grid.SetColumn(rightActions, 1);
        header.Children.Add(rightActions);
        return (header, runtimesFolderText, runtimeAdvancedToggleButton, runtimeCudaPreferenceCombo);
    }

    private static DataGrid InstalledRuntimesGrid(RuntimesPageRequest request)
    {
        var grid = PageSectionFactory.GridFor(
            (Loc.T("Runtimes.Col.Name"), nameof(RuntimeCatalogRow.Name), 1.4),
            (Loc.T("Runtimes.Col.Backend"), nameof(RuntimeCatalogRow.Backend), .55),
            (Loc.T("Runtimes.Col.State"), nameof(RuntimeCatalogRow.State), .55),
            (Loc.T("Runtimes.Col.Location"), nameof(RuntimeCatalogRow.Location), 3));
        grid.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.VisibleWhenSelected;
        grid.RowDetailsTemplate = PageSectionFactory.RowDetailsTemplate(nameof(RuntimeCatalogRow.Details));
        grid.PreviewMouseLeftButtonDown += request.Actions.RuntimeGridPreviewMouseLeftButtonDown;
        PageSectionFactory.AddButtonColumn(grid, Loc.T("Runtimes.ActionBtn.Build"), nameof(RuntimeCatalogRow.BuildAction), nameof(RuntimeCatalogRow.CanBuild), request.Actions.BuildRuntimeRowClick, .65, tooltipBinding: nameof(RuntimeCatalogRow.BuildToolTip));
        PageSectionFactory.AddButtonColumn(grid, Loc.T("Common.ActionButton"), nameof(RuntimeCatalogRow.DeleteAction), nameof(RuntimeCatalogRow.CanDelete), request.Actions.DeleteRuntimeRowClick, .65, tooltipBinding: nameof(RuntimeCatalogRow.DeleteToolTip));
        PageSectionFactory.ApplyGridTextMargin(grid, new Thickness(6, 0, 6, 0));
        request.Actions.ConfigureRuntimeGridColumnSizing(grid);
        grid.ItemsSource = request.ViewModel.Runtimes.Rows;
        return grid;
    }

    private static DataGrid RuntimePackageGrid(RuntimesPageRequest request)
    {
        var grid = PageSectionFactory.GridFor(
            (Loc.T("Runtimes.Col.Runtime"), nameof(RuntimePackagePresetRow.Label), 1.45),
            (Loc.T("Runtimes.Col.Backend"), nameof(RuntimePackagePresetRow.Backend), .68),
            (Loc.T("Runtimes.Col.Local"), nameof(RuntimePackagePresetRow.LocalStatus), .78),
            (Loc.T("Runtimes.Col.LatestRelease"), nameof(RuntimePackagePresetRow.LatestRelease), 1.2),
            (Loc.T("Runtimes.Col.Assets"), nameof(RuntimePackagePresetRow.Assets), 2.35));
        PageSectionFactory.AddButtonColumn(grid, Loc.T("Runtimes.ActionBtn.Install"), nameof(RuntimePackagePresetRow.InstallAction), nameof(RuntimePackagePresetRow.CanInstall), request.Actions.InstallRuntimePackageRowClick, .75, tooltipBinding: nameof(RuntimePackagePresetRow.InstallToolTip));
        PageSectionFactory.AddButtonColumn(grid, Loc.T("Runtimes.ActionBtn.Update"), nameof(RuntimePackagePresetRow.CheckAction), nameof(RuntimePackagePresetRow.CanCheck), request.Actions.CheckRuntimePackageUpdateRowClick, .75, tooltipBinding: nameof(RuntimePackagePresetRow.CheckToolTip));
        PageSectionFactory.AddButtonColumn(grid, Loc.T("Common.DeleteButton"), nameof(RuntimePackagePresetRow.DeleteAction), nameof(RuntimePackagePresetRow.CanDelete), request.Actions.DeleteRuntimePackageRowClick, .75, tooltipBinding: nameof(RuntimePackagePresetRow.DeleteToolTip));
        PageSectionFactory.ApplyGridTextMargin(grid, new Thickness(6, 0, 6, 0));
        request.Actions.ConfigureRuntimeBuildGridColumnSizing(grid);
        grid.ItemsSource = request.ViewModel.RuntimePackages.Rows;
        return grid;
    }

    private static DataGrid RuntimeBuildGrid(RuntimesPageRequest request)
    {
        var grid = PageSectionFactory.GridFor(
            (Loc.T("Runtimes.Col.Repository"), nameof(RuntimeBuildPresetRow.Label), 1.4),
            (Loc.T("Runtimes.Col.Backend"), nameof(RuntimeBuildPresetRow.Backend), .7),
            (Loc.T("Runtimes.Col.Local"), nameof(RuntimeBuildPresetRow.LocalStatus), .85),
            (Loc.T("Runtimes.Col.LatestLocal"), nameof(RuntimeBuildPresetRow.LatestLocal), 1.2),
            (Loc.T("Runtimes.Col.Source"), nameof(RuntimeBuildPresetRow.Source), 2.3));
        grid.IsReadOnly = false;
        PageSectionFactory.AddButtonColumn(grid, Loc.T("Runtimes.ActionBtn.Download"), nameof(RuntimeBuildPresetRow.DownloadAction), nameof(RuntimeBuildPresetRow.CanDownload), request.Actions.DownloadRuntimePresetRowClick, .75, tooltipBinding: nameof(RuntimeBuildPresetRow.DownloadToolTip));
        PageSectionFactory.AddButtonColumn(grid, Loc.T("Runtimes.ActionBtn.Update"), nameof(RuntimeBuildPresetRow.CheckAction), nameof(RuntimeBuildPresetRow.CanCheck), request.Actions.CheckRuntimePresetUpdateRowClick, .75, tooltipBinding: nameof(RuntimeBuildPresetRow.CheckToolTip));
        PageSectionFactory.AddButtonColumn(grid, Loc.T("Common.DeleteButton"), nameof(RuntimeBuildPresetRow.DeleteAction), nameof(RuntimeBuildPresetRow.CanDelete), request.Actions.DeleteRuntimePresetRowClick, .75, tooltipBinding: nameof(RuntimeBuildPresetRow.DeleteToolTip));
        PageSectionFactory.ApplyGridTextMargin(grid, new Thickness(6, 0, 6, 0));
        request.Actions.ConfigureRuntimeBuildGridColumnSizing(grid);
        grid.ItemsSource = request.ViewModel.RuntimeBuilds.Rows;
        return grid;
    }

    private static DataGrid RuntimeJobsGrid(RuntimesPageRequest request)
    {
        var grid = PageSectionFactory.GridFor(
            (Loc.T("Runtimes.Col.Status"), "C1", .8),
            (Loc.T("Runtimes.Col.Kind"), "C2", 1),
            (Loc.T("Runtimes.Col.Updated"), "C4", 1.1),
            (Loc.T("Runtimes.Col.Payload"), "C5", 3.2));
        PageSectionFactory.AddButtonColumn(grid, Loc.T("Common.LogButton"), "C6", "B1", request.Actions.OpenRuntimeJobLogRowClick, .55, tooltipBinding: "T1");
        PageSectionFactory.AddButtonColumn(grid, Loc.T("Runtimes.ActionBtn.Cancel"), "C7", "B2", request.Actions.CancelRuntimeJobRowClick, .7, tooltipBinding: "T2");
        PageSectionFactory.AddButtonColumn(grid, Loc.T("Runtimes.ActionBtn.Retry"), "C8", "B3", request.Actions.RetryRuntimeJobRowClick, .65, tooltipBinding: "T3");
        PageSectionFactory.AddButtonColumn(grid, Loc.T("Common.ClearButton"), "C9", "B4", request.Actions.ClearRuntimeJobRowClick, .65, tooltipBinding: "T4");
        PageSectionFactory.ApplyRuntimeJobsRowStyle(grid);
        request.Actions.ConfigureRuntimeJobsGridColumnSizing(grid);
        grid.ItemsSource = request.ViewModel.Jobs.RuntimeRows;
        return grid;
    }

    private static Grid FolderStripActionsFirst(string label, string path, out TextBlock pathText, params (string Text, Func<Task> Click)[] actions)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        var column = 0;
        foreach (var _ in actions)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        foreach (var action in actions)
        {
            var button = Button(action.Text, action.Click);
            Grid.SetColumn(button, column++);
            grid.Children.Add(button);
        }

        var labelBlock = new TextBlock
        {
            Text = label,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 10, 6)
        };
        Grid.SetColumn(labelBlock, column++);
        grid.Children.Add(labelBlock);

        pathText = new TextBlock
        {
            Text = path,
            Foreground = (WpfBrush)WpfApplication.Current.Resources["TextMuted"],
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 6)
        };
        Grid.SetColumn(pathText, column);
        grid.Children.Add(pathText);
        return grid;
    }

    private static WpfComboBox LaunchCombo(IEnumerable<string> values) => new()
    {
        ItemsSource = values.ToArray(),
        SelectedIndex = 0,
        MinHeight = 27,
        MinWidth = 76,
        Margin = new Thickness(0, 0, 6, 4),
        HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch
    };

    private static WpfButton Button(string text, Func<Task> click)
    {
        var button = new WpfButton { Content = text, ToolTip = TooltipText(ButtonToolTip(text)) };
        ToolTipService.SetShowOnDisabled(button, true);
        button.Click += async (_, _) => await click();
        return button;
    }

    private static string ButtonToolTip(string text)
        => (text ?? "").Trim() switch
        {
            var t when string.Equals(t, Loc.T("Runtimes.ChooseFolderButton")) => Loc.T("Tooltip.ChooseFolder"),
            var t when string.Equals(t, Loc.T("Runtimes.ShowAdvancedButton")) => Loc.T("Tooltip.RuntimesShowAdvanced"),
            var t when string.Equals(t, Loc.T("Runtimes.HideAdvancedButton")) => Loc.T("Tooltip.RuntimesHideAdvanced"),
            var label => string.IsNullOrWhiteSpace(label) ? "" : Loc.T("Common.RunAction", label)
        };

    private static string TooltipText(string text) => text;
}
