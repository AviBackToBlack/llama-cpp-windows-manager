namespace LocalLlmConsole.Services;

public sealed partial class LlamaProcessSupervisor
{
    public sealed record StopVerification(bool VerifiedStopped, string Error);

    public void Stop()
    {
        var result = StopVerified();
        if (!result.VerifiedStopped)
            Trace.TraceWarning($"Could not verify llama runtime shutdown: {result.Error}");
    }

    public StopVerification StopVerified()
    {
        var verified = true;
        var error = "";
        if (_lastRuntimeMode == RuntimeMode.Native)
        {
            var result = _nativeRuntimeStop.Stop(_process);
            verified = result.Exited;
            if (!verified)
                error = "The native runtime process remained alive after both stop attempts.";
        }
        else
        {
            verified = StopHostProcess();
            if (!verified)
                error = "The WSL host process remained alive after the stop attempt.";
        }

        if (_lastSettings is not null && _lastRuntimeMode == RuntimeMode.Wsl)
        {
            var wslResult = _wslRuntimeStop.StopAsync(new WslRuntimeStopRequest(
                _lastSettings,
                _lastRuntimeExecutablePath,
                _lastWslProcessMarker,
                LogPath,
                BoundedLogFile.MegabytesToBytes(_lastSettings.MaxLogFileSizeMb))).GetAwaiter().GetResult();
            verified &= wslResult.VerifiedStopped;
            if (!wslResult.VerifiedStopped)
                error = string.IsNullOrWhiteSpace(wslResult.Error)
                    ? "WSL could not verify that the runtime process stopped."
                    : wslResult.Error;
        }

        if (!verified)
            return new StopVerification(false, error);

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
        return new StopVerification(true, "");
    }

    private bool StopHostProcess()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(3000);
            }
            return _process is null || _process.HasExited;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Could not stop llama host process: {ex.Message}");
            try { return _process is null || _process.HasExited; }
            catch { return false; }
        }
    }

    public void Dispose() => Stop();
}
