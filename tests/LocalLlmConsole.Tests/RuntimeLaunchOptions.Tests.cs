using LocalLlmConsole.Models;
using LocalLlmConsole.Services;
using LocalLlmConsole.ViewModels;

namespace LocalLlmConsole.Tests;

public sealed class RuntimeLaunchOptionsTests
{
    [Fact]
    public async Task DiscoveryUsesCpuRuntimeHelpAndKeepsCpuSpecificOptions()
    {
        var root = Path.Combine(Path.GetTempPath(), "runtime-option-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var executable = Path.Combine(root, "llama-server.exe");
        await File.WriteAllBytesAsync(executable, [0], TestContext.Current.CancellationToken);
        var runner = new RecordingProcessRunner(new ProcessRunResult(1, """
              --threads N          generation threads
              --threads-batch N    prompt processing threads
              --cpu-mask M         CPU affinity mask
              --numa TYPE          NUMA strategy
            """, ""));
        var service = new RuntimeLaunchOptionDiscoveryService(runner);
        var runtime = new RuntimeChoice("cpu", "Official CPU", RuntimeBackend.Cpu, RuntimeMode.Native, executable, "Official CPU");

        var options = await service.DiscoverAsync(runtime, "", TestContext.Current.CancellationToken);

        Assert.DoesNotContain(options, option => option.Name == "--threads");
        Assert.Contains(options, option => option.Name == "--threads-batch");
        Assert.Contains(options, option => option.Name == "--cpu-mask");
        Assert.Contains(options, option => option.Name == "--numa");
        Assert.Equal(1, runner.CallCount);
    }

    [Fact]
    public async Task DiscoveryPersistsRuntimeHelpFingerprintVersionBannerAndParseOutcome()
    {
        var root = Path.Combine(Path.GetTempPath(), "runtime-option-diagnostics", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var executable = Path.Combine(root, "llama-server.exe");
        await File.WriteAllBytesAsync(executable, [0, 1, 2], TestContext.Current.CancellationToken);
        const string help = "llama-server version b9999\n  --cpu-mask M CPU affinity mask\n  --model PATH model";
        var runtime = new RuntimeChoice("official/cpu", "Official CPU", RuntimeBackend.Cpu, RuntimeMode.Native, executable, "Official CPU");
        var diagnostics = new RuntimeLaunchOptionDiagnosticsService(Path.Combine(root, "diagnostics"));
        var service = new RuntimeLaunchOptionDiscoveryService(
            new RecordingProcessRunner(new ProcessRunResult(0, help, "")),
            diagnostics);

        var options = await service.DiscoverAsync(runtime, "", TestContext.Current.CancellationToken);
        var diagnostic = System.Text.Json.JsonSerializer.Deserialize<RuntimeLaunchOptionDiagnostic>(
            await File.ReadAllTextAsync(diagnostics.DiagnosticPath(runtime), TestContext.Current.CancellationToken));

        Assert.Contains(options, option => option.Name == "--cpu-mask");
        Assert.NotNull(diagnostic);
        Assert.Equal("success", diagnostic.Status);
        Assert.Equal("llama-server version b9999", diagnostic.HelpBanner);
        Assert.Equal(2, diagnostic.ParsedOptionCount);
        Assert.Equal(1, diagnostic.RenderedOptionCount);
        Assert.Equal(64, diagnostic.HelpSha256.Length);
        Assert.Equal(3, diagnostic.ExecutableSizeBytes);
    }

    [Fact]
    public async Task DiscoveryRecordsChangedOrUnsupportedHelpFormat()
    {
        var root = Path.Combine(Path.GetTempPath(), "runtime-option-format", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var executable = Path.Combine(root, "llama-server.exe");
        await File.WriteAllBytesAsync(executable, [0], TestContext.Current.CancellationToken);
        var runtime = new RuntimeChoice("cpu", "CPU", RuntimeBackend.Cpu, RuntimeMode.Native, executable, "CPU");
        var diagnostics = new RuntimeLaunchOptionDiagnosticsService(Path.Combine(root, "diagnostics"));
        var service = new RuntimeLaunchOptionDiscoveryService(
            new RecordingProcessRunner(new ProcessRunResult(0, "new help format without option markers", "")),
            diagnostics);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DiscoverAsync(runtime, "", TestContext.Current.CancellationToken));
        var diagnostic = System.Text.Json.JsonSerializer.Deserialize<RuntimeLaunchOptionDiagnostic>(
            await File.ReadAllTextAsync(diagnostics.DiagnosticPath(runtime), TestContext.Current.CancellationToken));

        Assert.Contains("help format changed or is unsupported", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(diagnostic);
        Assert.Equal("unrecognized-help", diagnostic.Status);
        Assert.Equal(0, diagnostic.ParsedOptionCount);
    }

    [Fact]
    public async Task DiscoveryReportsMissingRuntimeBeforeStartingAProcess()
    {
        var runner = new RecordingProcessRunner(new ProcessRunResult(0, "", ""));
        var service = new RuntimeLaunchOptionDiscoveryService(runner);
        var runtime = new RuntimeChoice(
            "missing",
            "Missing CPU",
            RuntimeBackend.Cpu,
            RuntimeMode.Native,
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "llama-server.exe"),
            "Missing CPU");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DiscoverAsync(runtime, "", TestContext.Current.CancellationToken));

        Assert.Contains("missing", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Repair or reinstall", error.Message, StringComparison.Ordinal);
        Assert.Equal(0, runner.CallCount);
    }

    [Fact]
    public void HelpParserPreservesExactAliasesAndInfersChoices()
    {
        const string help = """
              -c,    --ctx-size N            context size
                  --flash-attn [auto|on|off] flash attention mode (default: auto)
                  --slot-save-path PATH      save slots here
                  --metrics                  enable metrics (default: false)
            """;

        var options = RuntimeLaunchHelpParser.Parse(help);

        var context = Assert.Single(options, option => option.Name == "--ctx-size");
        Assert.Equal(["-c", "--ctx-size"], context.Aliases);
        Assert.Equal(RuntimeLaunchOptionValueKind.Text, context.ValueKind);
        var flash = Assert.Single(options, option => option.Name == "--flash-attn");
        Assert.Equal(RuntimeLaunchOptionValueKind.Choice, flash.ValueKind);
        Assert.Equal(["auto", "on", "off"], flash.Choices);
        Assert.Equal("auto", flash.DefaultValue);
        var metrics = Assert.Single(options, option => option.Name == "--metrics");
        Assert.Equal(RuntimeLaunchOptionValueKind.Switch, metrics.ValueKind);
        Assert.Equal("false", metrics.DefaultValue);
    }

    [Fact]
    public void PolicyOnlyRendersSafeUnmanagedOptions()
    {
        var parsed = RuntimeLaunchHelpParser.Parse("""
              --model PATH             model
              --host HOST              listener
              --slot-save-path PATH    slot directory
              --help                   show help
            """);

        var rendered = parsed.Where(RuntimeLaunchOptionPolicy.CanRender).Select(option => option.Name).ToArray();

        Assert.Equal(["--slot-save-path"], rendered);
    }

    [Theory]
    [InlineData("--model")]
    [InlineData("--model=other.gguf")]
    [InlineData("--port")]
    [InlineData("--api-key")]
    public void ManagedArgumentsCannotBeOverridden(string argument)
    {
        var error = Assert.Throws<InvalidOperationException>(() => RuntimeLaunchOptionPolicy.ValidateCustomArguments([argument]));
        Assert.Contains("managed by the application", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PreviewUsesTheSameRuntimeAdapterAsLaunch()
    {
        var root = Path.Combine(Path.GetTempPath(), "runtime-preview-tests");
        var settings = AppSettings.CreateDefault(root) with
        {
            ContextSize = 8192,
            Temperature = 0.4,
            CustomParameters = "--slot-save-path \"C:\\slot cache\""
        };
        var runtime = new RuntimeChoice("runtime", "Runtime", RuntimeBackend.Cpu, RuntimeMode.Native, "llama-server.exe");

        var preview = RuntimeLaunchRequestFactory.Preview(settings, runtime);

        Assert.Contains("--model <model.gguf>", preview, StringComparison.Ordinal);
        Assert.Contains("--ctx-size 8192", preview, StringComparison.Ordinal);
        Assert.Contains("--temp 0.4", preview, StringComparison.Ordinal);
        Assert.Contains("--slot-save-path \"C:\\\\slot cache\"", preview, StringComparison.Ordinal);
    }

    [Fact]
    public void CuratedSchemaHasUniqueValidSettingsAndChoiceMetadata()
    {
        var definitions = LaunchSettingUiSchema.Definitions;
        Assert.Equal(definitions.Count, definitions.Select(definition => definition.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(definitions, definition => Assert.NotNull(typeof(AppSettings).GetProperty(definition.Id)));
        Assert.All(definitions.Where(definition => definition.Editor == LaunchSettingEditorKind.Choice),
            definition => Assert.NotEmpty(definition.Choices ?? []));
    }

    private sealed class RecordingProcessRunner(ProcessRunResult result) : IProcessRunner
    {
        public int CallCount { get; private set; }

        public Task<ProcessRunResult> RunAsync(
            System.Diagnostics.ProcessStartInfo psi,
            TimeSpan timeout,
            CancellationToken cancellationToken = default,
            string? standardInput = null)
        {
            CallCount++;
            return Task.FromResult(result);
        }
    }
}
