using LocalLlmConsole.Models;
using LocalLlmConsole.Services;

namespace LocalLlmConsole.Tests;

public sealed class LaunchCommandServiceTests : IDisposable
{
    private readonly string _modelFile = Path.GetTempFileName();

    public LaunchCommandServiceTests()
    {
        File.WriteAllText(_modelFile, "placeholder");
    }

    public void Dispose()
    {
        try { File.Delete(_modelFile); } catch { }
    }

    [Fact]
    public void BuildCommandAndParseRoundTripMatchFlags()
    {
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = _modelFile,
            Backend = RuntimeBackend.Cuda,
            GpuLayers = 99,
            ContextSize = 8192,
            ParallelSlots = 2,
            BatchSize = 2048,
            MicroBatchSize = 512,
            Threads = 4,
            FlashAttention = "on",
            CacheTypeK = "q8_0",
            CacheTypeV = "q8_0",
            KvOffload = "on",
            KvUnified = "off",
            Temperature = 0.8,
            TopK = 40,
            TopP = 0.95,
            MinP = 0.05,
            MaxTokens = 512,
            Seed = 42,
            RepeatLastN = 64,
            RepeatPenalty = 1.1,
            PresencePenalty = 0.2,
            FrequencyPenalty = -0.1,
            RopeScaling = "linear",
            RopeScale = 0.5,
            RopeFreqBase = 10000,
            RopeFreqScale = 0.9,
            ContinuousBatching = "on",
            MmapMode = "on",
            MlockMode = "on"
        };

        var command = LaunchCommandService.BuildCommand(options);
        var parsed = LaunchCommandService.ParseCommand(command);

        Assert.Empty(parsed.Errors);
        Assert.Empty(parsed.SecurityWarnings);
        Assert.Equal(_modelFile, parsed.Flags["--model"]);
        Assert.Equal("8192", parsed.Flags["--ctx-size"]);
        Assert.Equal("99", parsed.Flags["--n-gpu-layers"]);
        Assert.Equal("2", parsed.Flags["--parallel"]);
        Assert.Equal("2048", parsed.Flags["--batch-size"]);
        Assert.Equal("512", parsed.Flags["--ubatch-size"]);
        Assert.Equal("4", parsed.Flags["--threads"]);
        Assert.Equal("true", parsed.Flags["--flash-attn"]);
        Assert.Equal("q8_0", parsed.Flags["--cache-type-k"]);
        Assert.Equal("q8_0", parsed.Flags["--cache-type-v"]);
        Assert.Equal("true", parsed.Flags["--kv-offload"]);
        Assert.Equal("false", parsed.Flags["--kv-unified"]);
        Assert.Equal("0.8", parsed.Flags["--temp"]);
        Assert.Equal("40", parsed.Flags["--top-k"]);
        Assert.Equal("0.95", parsed.Flags["--top-p"]);
        Assert.Equal("0.05", parsed.Flags["--min-p"]);
        Assert.Equal("512", parsed.Flags["--predict"]);
        Assert.Equal("42", parsed.Flags["--seed"]);
        Assert.Equal("64", parsed.Flags["--repeat-last-n"]);
        Assert.Equal("1.1", parsed.Flags["--repeat-penalty"]);
        Assert.Equal("0.2", parsed.Flags["--presence-penalty"]);
        Assert.Equal("-0.1", parsed.Flags["--frequency-penalty"]);
        Assert.Equal("linear", parsed.Flags["--rope-scaling"]);
        Assert.Equal("0.5", parsed.Flags["--rope-scale"]);
        Assert.Equal("10000", parsed.Flags["--rope-freq-base"]);
        Assert.Equal("0.9", parsed.Flags["--rope-freq-scale"]);
        Assert.Equal("true", parsed.Flags["--cont-batching"]);
        Assert.Equal("true", parsed.Flags["--mmap"]);
        Assert.Equal("true", parsed.Flags["--mlock"]);
    }

    [Fact]
    public void ParseCommandExtractsCommonFlagsAndExtraArgs()
    {
        var command = $"--model \"{_modelFile}\" --ctx-size 4096 --threads 4 --flash-attn on --temp 0.7 --top-p 0.9 --verbose unknown-value";

        var parsed = LaunchCommandService.ParseCommand(command);

        Assert.Empty(parsed.Errors);
        Assert.Equal(_modelFile, parsed.Flags["--model"]);
        Assert.Equal("4096", parsed.Flags["--ctx-size"]);
        Assert.Equal("4", parsed.Flags["--threads"]);
        Assert.Equal("true", parsed.Flags["--flash-attn"]);
        Assert.Equal("0.7", parsed.Flags["--temp"]);
        Assert.Equal("0.9", parsed.Flags["--top-p"]);
        Assert.Equal("true", parsed.Flags["--verbose"]);
        Assert.Contains("unknown-value", parsed.ExtraArgs);
    }

    [Fact]
    public void ParseCommandRejectsSecurityCriticalFlags()
    {
        var command = $"--model \"{_modelFile}\" --host 0.0.0.0 --port 9090 --api-key secret";

        var parsed = LaunchCommandService.ParseCommand(command);

        Assert.Empty(parsed.Errors);
        Assert.Contains(parsed.SecurityWarnings, w => w.Contains("--host", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(parsed.SecurityWarnings, w => w.Contains("--port", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(parsed.SecurityWarnings, w => w.Contains("--api-key", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("--host", parsed.Flags.Keys);
        Assert.DoesNotContain("--port", parsed.Flags.Keys);
        Assert.DoesNotContain("--api-key", parsed.Flags.Keys);
        Assert.Equal(_modelFile, parsed.Flags["--model"]);
    }

    [Fact]
    public void ParseCommandHandlesBooleanNegationAndValues()
    {
        var parsed = LaunchCommandService.ParseCommand("--mmap --no-mmap --kv-offload on --kv-offload off --kv-unified auto");

        Assert.Empty(parsed.Errors);
        Assert.Equal("false", parsed.Flags["--mmap"]);
        Assert.Equal("false", parsed.Flags["--kv-offload"]);
        Assert.Equal("true", parsed.Flags["--kv-unified"]);
    }

    [Fact]
    public void BuildCommandQuotesPathsWithSpaces()
    {
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = "C:\\Models\\my model.gguf",
            ContextSize = 4096
        };

        var command = LaunchCommandService.BuildCommand(options);

        Assert.Contains("\"C:\\\\Models\\\\my model.gguf\"", command);
    }

    [Fact]
    public void ParseCommandHandlesEqualsSyntax()
    {
        var parsed = LaunchCommandService.ParseCommand($"--model=\"{_modelFile}\" --ctx-size=4096 --threads=4 --flash-attn=on");

        Assert.Empty(parsed.Errors);
        Assert.Equal(_modelFile, parsed.Flags["--model"]);
        Assert.Equal("4096", parsed.Flags["--ctx-size"]);
        Assert.Equal("4", parsed.Flags["--threads"]);
        Assert.Equal("true", parsed.Flags["--flash-attn"]);
    }

    [Fact]
    public void GenericFlagValuesOverrideWhenNotFirstClass()
    {
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = _modelFile,
            ContextSize = 4096,
            FlagValues = new Dictionary<string, string> { ["--cpu-mask"] = "0xF" }
        };

        var command = LaunchCommandService.BuildCommand(options);

        Assert.Contains("--cpu-mask", command);
        Assert.Contains("0xF", command);
    }

    [Fact]
    public void FirstClassWinsOverFlagValues()
    {
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = _modelFile,
            ContextSize = 4096,
            Temperature = 0.8,
            FlagValues = new Dictionary<string, string> { ["--temp"] = "0.99" }
        };

        var command = LaunchCommandService.BuildCommand(options);
        var parsed = LaunchCommandService.ParseCommand(command);

        Assert.Equal("0.8", parsed.Flags["--temp"]);
    }

    [Fact]
    public void BuildCommandEmitsNegatedBooleanWhenValueIsFalse()
    {
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = _modelFile,
            MmapMode = "off"
        };

        var command = LaunchCommandService.BuildCommand(options);

        Assert.Contains("--no-mmap", command);
        Assert.DoesNotContain("--mmap", command);
    }

    [Fact]
    public void BuildCommandOmitsUnsupportedFlagsWhenSupportedFlagsProvided()
    {
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = _modelFile,
            ContextSize = 4096,
            Threads = 4,
            FlashAttention = "on",
            SupportedFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "--model", "--ctx-size" }
        };

        var command = LaunchCommandService.BuildCommand(options);

        Assert.Contains("--model", command);
        Assert.Contains("--ctx-size", command);
        Assert.DoesNotContain("--threads", command);
        Assert.DoesNotContain("--flash-attn", command);
    }

    [Fact]
    public void ParseCommandFiltersUnsupportedFlagsWhenSupportedFlagsProvided()
    {
        var command = $"--model \"{_modelFile}\" --ctx-size 4096 --threads 4 --flash-attn on";
        var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "--model", "--ctx-size" };

        var parsed = LaunchCommandService.ParseCommand(command, supported);

        Assert.Empty(parsed.Errors);
        Assert.Equal(_modelFile, parsed.Flags["--model"]);
        Assert.Equal("4096", parsed.Flags["--ctx-size"]);
        Assert.Contains("--threads", parsed.ExtraArgs);
        Assert.Contains("4", parsed.ExtraArgs);
        Assert.Contains("--flash-attn", parsed.ExtraArgs);
        Assert.Contains("on", parsed.ExtraArgs);
    }

    [Fact]
    public void ParseCommandPreservesQuotedValueWithSpaces()
    {
        var path = "C:\\Models\\my model.gguf";
        var command = $"--model \"{path}\" --ctx-size 4096";

        var parsed = LaunchCommandService.ParseCommand(command);

        Assert.Empty(parsed.Errors);
        Assert.Equal(path, parsed.Flags["--model"]);
        Assert.Equal("4096", parsed.Flags["--ctx-size"]);
    }

    [Fact]
    public void ParseCommandRejectsSecurityCriticalFlagsWithEqualsSyntax()
    {
        var command = $"--model \"{_modelFile}\" --host=0.0.0.0 --port=9090 --api-key=secret";

        var parsed = LaunchCommandService.ParseCommand(command);

        Assert.Empty(parsed.Errors);
        Assert.Contains(parsed.SecurityWarnings, w => w.Contains("--host", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(parsed.SecurityWarnings, w => w.Contains("--port", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(parsed.SecurityWarnings, w => w.Contains("--api-key", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("--host", parsed.Flags.Keys);
        Assert.DoesNotContain("--port", parsed.Flags.Keys);
        Assert.DoesNotContain("--api-key", parsed.Flags.Keys);
    }

    [Fact]
    public void ParseCommandFiltersNegatedBooleanByRuntimeSupport()
    {
        var supportedFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "--mlock" };

        var parsed = LaunchCommandService.ParseCommand("--no-mlock", supportedFlags);

        Assert.Empty(parsed.Errors);
        Assert.DoesNotContain("--mlock", parsed.Flags.Keys);
        Assert.Contains("--no-mlock", parsed.ExtraArgs);
    }
}
