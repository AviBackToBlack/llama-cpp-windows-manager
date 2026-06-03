using System.Diagnostics;

namespace LocalLlmConsole.Services;

public sealed class ShellIntegrationService
{
    private readonly Action<ProcessStartInfo> _startProcess;

    public ShellIntegrationService(Action<ProcessStartInfo> startProcess)
    {
        _startProcess = startProcess ?? throw new ArgumentNullException(nameof(startProcess));
    }

    public void OpenFolder(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        Directory.CreateDirectory(path);
        OpenPath(path);
    }

    public void OpenPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = ResolveExistingShellPath(path);
        _startProcess(new ProcessStartInfo(fullPath) { UseShellExecute = true });
    }

    public void OpenUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("URL must be an absolute HTTP or HTTPS URL.", nameof(url));
        }

        _startProcess(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private static string ResolveExistingShellPath(string path)
    {
        var trimmed = path.Trim();
        string fullPath;
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            if (!uri.IsFile)
                throw new ArgumentException("Path must be a local file or directory path.", nameof(path));
            fullPath = Path.GetFullPath(uri.LocalPath);
        }
        else
        {
            fullPath = Path.GetFullPath(trimmed);
        }

        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            throw new FileNotFoundException("Path does not exist.", fullPath);
        return fullPath;
    }
}
