using System.Diagnostics;
using System.Text;

namespace LocalLlmConsole.Services;

/// <summary>Runs llama-server --help for a native Windows or WSL runtime.</summary>
public interface IRuntimeFlagHelpRunner
{
    Task<ProcessRunResult> RunHelpAsync(string executablePath, RuntimeMode mode, string? wslDistro, CancellationToken cancellationToken = default);
}

/// <summary>Runs llama-server --help and captures the output for capability detection.</summary>
public sealed class RuntimeFlagHelpRunner : IRuntimeFlagHelpRunner
{
    private readonly IProcessRunner _processRunner;
    private readonly Func<string> _wslExecutablePath;

    public RuntimeFlagHelpRunner(IProcessRunner processRunner)
        : this(processRunner, HostExecutableResolver.WslExe)
    {
    }

    public RuntimeFlagHelpRunner(IProcessRunner processRunner, Func<string>? wslExecutablePath)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _wslExecutablePath = wslExecutablePath ?? HostExecutableResolver.WslExe;
    }

    public async Task<ProcessRunResult> RunHelpAsync(string executablePath, RuntimeMode mode, string? wslDistro, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
            return new ProcessRunResult(0, string.Empty, string.Empty);

        if (mode == RuntimeMode.Wsl)
        {
            if (string.IsNullOrWhiteSpace(wslDistro))
                return new ProcessRunResult(0, string.Empty, string.Empty);

            var wslPath = RuntimePackageWslFileService.WindowsPathToWslPath(executablePath);
            var command = new StringBuilder();
            command.Append(CommandLineService.BashQuote(wslPath));
            command.Append(" --help");

            var psi = new ProcessStartInfo(_wslExecutablePath())
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var arg in new[] { "-d", wslDistro, "--", "bash", "-lc", command.ToString() })
                psi.ArgumentList.Add(arg);

            return await _processRunner.RunAsync(psi, TimeSpan.FromSeconds(30), cancellationToken);
        }

        var nativePsi = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(executablePath) ?? Environment.CurrentDirectory
        };
        nativePsi.ArgumentList.Add("--help");

        return await _processRunner.RunAsync(nativePsi, TimeSpan.FromSeconds(30), cancellationToken);
    }
}
