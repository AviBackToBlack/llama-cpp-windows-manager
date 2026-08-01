using System.Collections.ObjectModel;

namespace LocalLlmConsole.ViewModels;

public sealed record GatewayRoutingOverviewStatus(
    bool Visible,
    bool Enabled,
    string Endpoint,
    string State,
    string Policy,
    string Exposure,
    int RunningSessions)
{
    public static GatewayRoutingOverviewStatus Hidden { get; } = new(false, false, "", "", "", "", 0);

    public static GatewayRoutingOverviewStatus FromEndpoint(string endpoint)
        => string.IsNullOrWhiteSpace(endpoint)
            ? Hidden
            : new(true, true, endpoint.Trim(), "Listening", "", "", 0);
}

public sealed record OverviewLaunchProfileChoice(string Id, string Name);

public sealed class OverviewPageViewModel
{
    public ObservableCollection<ModelRecord> ModelChoices { get; } = new();
    public ObservableCollection<OverviewLaunchProfileChoice> LaunchProfileChoices { get; } = new();
    public ObservableCollection<UiRow> SessionRows { get; } = new();

    public void ReplaceModels(IEnumerable<ModelRecord> models)
    {
        ModelChoices.Clear();
        foreach (var model in models
                     .Where(model => !ModelAliasService.IsLaunchAlias(model))
                     .OrderBy(model => model.Name, StringComparer.OrdinalIgnoreCase))
            ModelChoices.Add(model);
    }

    public void ReplaceLaunchProfiles(IEnumerable<NamedModelLaunchProfile> profiles)
    {
        LaunchProfileChoices.Clear();
        foreach (var profile in profiles
                     .OrderByDescending(profile => profile.IsDefault)
                     .ThenBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase))
            LaunchProfileChoices.Add(new OverviewLaunchProfileChoice(profile.Id, profile.Name));
    }

    public void ReplaceSessions(IEnumerable<LoadedModelSessionSnapshot> sessions, string gatewayEndpoint = "")
        => _ = ReplaceSessionsIfChanged(sessions, GatewayRoutingOverviewStatus.FromEndpoint(gatewayEndpoint));

    public bool ReplaceSessionsIfChanged(IEnumerable<LoadedModelSessionSnapshot> sessions, string gatewayEndpoint = "")
        => ReplaceSessionsIfChanged(sessions, GatewayRoutingOverviewStatus.FromEndpoint(gatewayEndpoint));

    public void ReplaceSessions(IEnumerable<LoadedModelSessionSnapshot> sessions, GatewayRoutingOverviewStatus gateway)
        => _ = ReplaceSessionsIfChanged(sessions, gateway);

    public bool ReplaceSessionsIfChanged(IEnumerable<LoadedModelSessionSnapshot> sessions, GatewayRoutingOverviewStatus gateway)
    {
        var sessionRows = sessions.ToArray();
        var rows = BuildSessionRows(sessionRows, gateway).ToArray();
        if (RowsEqual(SessionRows, rows)) return false;

        SessionRows.Clear();
        foreach (var row in rows)
            SessionRows.Add(row);
        return true;
    }

    private static IEnumerable<UiRow> BuildSessionRows(IReadOnlyList<LoadedModelSessionSnapshot> sessions, GatewayRoutingOverviewStatus gateway)
    {
        if (gateway.Visible)
            yield return GatewayRow(gateway);

        foreach (var session in sessions.OrderByDescending(session => session.IsSelected).ThenBy(session => session.ModelName, StringComparer.OrdinalIgnoreCase))
        {
            yield return new UiRow
            {
                C1 = session.IsSelected ? $"{session.ModelName} (selected)" : session.ModelName,
                C2 = string.IsNullOrWhiteSpace(session.LaunchProfileName) ? "Unknown" : session.LaunchProfileName,
                C3 = session.ModelSize,
                C4 = SessionStatusLabel(session),
                C5 = EndpointLabel(session, gateway),
                C6 = session.RuntimeName,
                C7 = $"{session.Backend} {session.Mode}",
                C8 = session.IsRunning && session.Status != LoadedModelSessionStatus.Stopping ? "Unload" : "",
                B1 = session.IsRunning && session.Status != LoadedModelSessionStatus.Stopping,
                Data = JsonSerializer.SerializeToNode(new { session.SessionId, session.ModelId }) as JsonObject ?? new JsonObject()
            };
        }
    }

    private static UiRow GatewayRow(GatewayRoutingOverviewStatus gateway)
        => new()
        {
            C1 = gateway.Enabled ? "Gateway (shared endpoint)" : "Gateway (off)",
            C2 = "—",
            C3 = "Shared router",
            C4 = string.IsNullOrWhiteSpace(gateway.State) ? (gateway.Enabled ? "Enabled" : "Off") : gateway.State,
            C5 = gateway.Enabled
                ? $"Shared: {gateway.Endpoint}{Environment.NewLine}Routes by model id to {gateway.RunningSessions.ToString(CultureInfo.InvariantCulture)} loaded model endpoint(s)."
                : "Gateway disabled",
            C6 = string.IsNullOrWhiteSpace(gateway.Policy) ? "" : gateway.Policy,
            C7 = string.IsNullOrWhiteSpace(gateway.Exposure) ? "" : gateway.Exposure,
            B1 = false,
            Data = JsonSerializer.SerializeToNode(new { Kind = "Gateway" }) as JsonObject ?? new JsonObject()
        };

    private static bool RowsEqual(IReadOnlyList<UiRow> left, IReadOnlyList<UiRow> right)
    {
        if (left.Count != right.Count) return false;
        for (var i = 0; i < left.Count; i++)
        {
            if (!RowEquals(left[i], right[i])) return false;
        }

        return true;
    }

    private static bool RowEquals(UiRow left, UiRow right)
        => string.Equals(left.C1, right.C1, StringComparison.Ordinal)
           && string.Equals(left.C2, right.C2, StringComparison.Ordinal)
           && string.Equals(left.C3, right.C3, StringComparison.Ordinal)
           && string.Equals(left.C4, right.C4, StringComparison.Ordinal)
           && string.Equals(left.C5, right.C5, StringComparison.Ordinal)
           && string.Equals(left.C6, right.C6, StringComparison.Ordinal)
           && string.Equals(left.C7, right.C7, StringComparison.Ordinal)
           && string.Equals(left.C8, right.C8, StringComparison.Ordinal)
           && string.Equals(left.C9, right.C9, StringComparison.Ordinal)
           && string.Equals(left.C10, right.C10, StringComparison.Ordinal)
           && string.Equals(left.T1, right.T1, StringComparison.Ordinal)
           && string.Equals(left.T2, right.T2, StringComparison.Ordinal)
           && string.Equals(left.T3, right.T3, StringComparison.Ordinal)
           && string.Equals(left.T4, right.T4, StringComparison.Ordinal)
           && string.Equals(left.T5, right.T5, StringComparison.Ordinal)
           && left.B1 == right.B1
           && left.B2 == right.B2
           && left.B3 == right.B3
           && left.B4 == right.B4
           && left.B5 == right.B5
           && JsonNode.DeepEquals(left.Data, right.Data);

    private static string SessionStatusLabel(LoadedModelSessionSnapshot session) => session.Status switch
    {
        LoadedModelSessionStatus.Running or LoadedModelSessionStatus.Warm => "Loaded",
        LoadedModelSessionStatus.Loading => "Loading",
        LoadedModelSessionStatus.Unreachable => "Unreachable",
        LoadedModelSessionStatus.Stopping => "Stopping",
        LoadedModelSessionStatus.Failed => string.IsNullOrWhiteSpace(session.StatusReason) ? "Failed" : $"Failed — {session.StatusReason}",
        _ => string.IsNullOrWhiteSpace(session.StatusReason) ? "Unloaded" : $"Unloaded — {session.StatusReason}"
    };

    private static string EndpointLabel(LoadedModelSessionSnapshot session, GatewayRoutingOverviewStatus gateway)
    {
        if (!gateway.Visible || !gateway.Enabled)
            return $"Direct: {session.Endpoint}";
        return $"Direct: {session.Endpoint}{Environment.NewLine}Also available via gateway: {gateway.Endpoint}";
    }
}
