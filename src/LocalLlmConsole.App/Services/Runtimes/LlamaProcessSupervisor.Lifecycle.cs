namespace LocalLlmConsole.Services;

public sealed partial class LlamaProcessSupervisor
{
    public void Stop()
    {
        if (_lastRuntimeMode == RuntimeMode.Native)
            _nativeRuntimeStop.Stop(_process);
        else
            StopHostProcess();

        if (_lastSettings is not null && _lastRuntimeMode == RuntimeMode.Wsl)
        {
            _wslRuntimeStop.Stop(new WslRuntimeStopRequest(
                _lastSettings,
                _lastRuntimeExecutablePath,
                _lastWslProcessMarker,
                LogPath,
                BoundedLogFile.MegabytesToBytes(_lastSettings.MaxLogFileSizeMb)));
        }

        try { _process?.Dispose(); }
        catch (Exception ex) { Trace.TraceWarning($"Could not dispose llama process handle: {ex.Message}"); }
        try { _jobObject?.Dispose(); }
        catch (Exception ex) { Trace.TraceWarning($"Could not dispose llama job object: {ex.Message}"); }
        try { _log?.Dispose(); }
        catch (Exception ex) { Trace.TraceWarning($"Could not dispose llama log writer: {ex.Message}"); }
        _process = null;
        _jobObject = null;
        _log = null;
        ActiveModelId = "";
        ActiveRuntimeId = "";
        State = LlamaRuntimeState.Stopped;
        LastExitCode = null;
        _lastSettings = null;
        _lastRuntimeExecutablePath = "";
        _lastWslProcessMarker = "";
        _lastApiKey = "";
        _attached = false;
        _recovered = false;
    }

    private void StopHostProcess()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(3000);
            }
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Could not stop llama host process: {ex.Message}");
        }
    }

    public void Dispose() => Stop();
}
