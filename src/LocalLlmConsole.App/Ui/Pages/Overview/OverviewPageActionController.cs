namespace LocalLlmConsole;

public sealed record OverviewPageActionControllerActions(
    Func<Task> SelectModelSessionAsync,
    Func<Task> SelectLaunchProfileAsync,
    Action UpdateModelActions,
    Func<Task> LoadSelectedModelAsync,
    Func<Task> SelectLoadedSessionRowAsync,
    Func<object, string> SessionIdFromRowButton,
    Func<string, Task> UnloadLoadedSessionAsync,
    Func<Func<Task>, Task> RunEventAsync);

public sealed class OverviewPageActionController
{
    private readonly OverviewPageActionControllerActions _actions;

    public OverviewPageActionController(OverviewPageActionControllerActions actions)
    {
        _actions = actions;
    }

    public OverviewPageActions Build()
        => new(
            SelectModelSessionAsync,
            _actions.SelectLaunchProfileAsync,
            _actions.LoadSelectedModelAsync,
            async () => await _actions.RunEventAsync(_actions.SelectLoadedSessionRowAsync),
            UnloadLoadedSessionRow_Click);

    private async Task SelectModelSessionAsync()
    {
        await _actions.SelectModelSessionAsync();
        _actions.UpdateModelActions();
    }

    private async void UnloadLoadedSessionRow_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        await _actions.RunEventAsync(async () =>
        {
            var sessionId = _actions.SessionIdFromRowButton(sender);
            if (!string.IsNullOrWhiteSpace(sessionId))
                await _actions.UnloadLoadedSessionAsync(sessionId);
        });
    }
}
