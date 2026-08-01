using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace LocalLlmConsole;

public sealed record HuggingFaceGridModeActions(
    RoutedEventHandler DownloadSearchRow,
    RoutedEventHandler OpenModelCardRow,
    RoutedEventHandler ResumeDownloadRow,
    RoutedEventHandler PauseDownloadRow,
    RoutedEventHandler StopDownloadRow,
    RoutedEventHandler DeleteDownloadRow);

public sealed record HuggingFaceGridModeRequest(
    DataGrid Grid,
    IEnumerable SearchRows,
    IEnumerable DownloadHistoryRows,
    HuggingFaceGridModeActions Actions,
    Action<DataGrid> ConfigureSearchColumnSizing,
    Action<DataGrid> ConfigureDownloadHistoryColumnSizing);

public static class HuggingFaceGridModeFactory
{
    public static void ConfigureSearch(HuggingFaceGridModeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Grid);
        ArgumentNullException.ThrowIfNull(request.Actions);

        PageSectionFactory.ConfigureGridColumns(
            request.Grid,
            (Loc.T("HfSearch.Col.Repo"), "C1", 1.3),
            (Loc.T("HfSearch.Col.File"), "C2", 2.3),
            (Loc.T("HfSearch.Col.Quant"), "C3", .6),
            (Loc.T("HfSearch.Col.Size"), "C4", .8),
            (Loc.T("HfSearch.Col.Downloads"), "C5", .8),
            (Loc.T("HfSearch.Col.Signals"), "C6", 1.4));
        PageSectionFactory.AddButtonColumn(request.Grid, Loc.T("HfSearch.Col.Actions"), "C7", "B1", request.Actions.DownloadSearchRow, .8, tooltipBinding: "T1", visualRole: VisualRole.Primary);
        PageSectionFactory.AddButtonColumn(request.Grid, Loc.T("HfSearch.Col.Card"), "C8", "B2", request.Actions.OpenModelCardRow, .6, tooltipBinding: "T2");
        PageSectionFactory.ApplyGridTextMargin(request.Grid, new Thickness(6, 0, 6, 0));
        request.ConfigureSearchColumnSizing(request.Grid);
        request.Grid.SelectedItem = null;
        request.Grid.ItemsSource = request.SearchRows;
    }

    public static void ConfigureDownloadHistory(HuggingFaceGridModeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Grid);
        ArgumentNullException.ThrowIfNull(request.Actions);

        PageSectionFactory.ConfigureGridColumns(
            request.Grid,
            (Loc.T("DownloadHistory.Col.Status"), "C1", .8),
            (Loc.T("DownloadHistory.Col.Model"), "C2", 2.1),
            (Loc.T("DownloadHistory.Col.Progress"), "C3", 1.1),
            (Loc.T("DownloadHistory.Col.Size"), "C4", .8),
            (Loc.T("DownloadHistory.Col.Updated"), "C5", 1),
            (Loc.T("DownloadHistory.Col.Destination"), "C6", 2.4));
        PageSectionFactory.AddButtonColumn(request.Grid, Loc.T("DownloadHistory.Action.Start"), "C7", "B1", request.Actions.ResumeDownloadRow, .7, tooltipBinding: "T1", visualRole: VisualRole.Primary);
        PageSectionFactory.AddButtonColumn(request.Grid, Loc.T("DownloadHistory.Action.Pause"), "C8", "B2", request.Actions.PauseDownloadRow, .7, tooltipBinding: "T2");
        PageSectionFactory.AddButtonColumn(request.Grid, Loc.T("DownloadHistory.Action.Stop"), "C9", "B3", request.Actions.StopDownloadRow, .7, tooltipBinding: "T3", visualRole: VisualRole.Danger);
        PageSectionFactory.AddButtonColumn(request.Grid, Loc.T("Common.DeleteButton"), "C10", "B4", request.Actions.DeleteDownloadRow, .7, tooltipBinding: "T4", visualRole: VisualRole.Danger);
        PageSectionFactory.ApplyGridTextMargin(request.Grid, new Thickness(6, 0, 6, 0));
        request.ConfigureDownloadHistoryColumnSizing(request.Grid);
        request.Grid.SelectedItem = null;
        request.Grid.ItemsSource = request.DownloadHistoryRows;
    }
}
