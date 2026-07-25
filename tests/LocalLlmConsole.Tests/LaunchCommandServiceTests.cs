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
            BatchSize = 4096,
            MicroBatchSize = 1024,
            Threads = 4,
            FlashAttention = "on",
            CacheTypeK = "q8_0",
            CacheTypeV = "q8_0",
            KvOffload = "off",
            KvUnified = "off",
            Temperature = 0.7,
            TopK = 50,
            TopP = 0.9,
            MinP = 0.1,
            MaxTokens = 512,
            Seed = 42,
            RepeatLastN = 32,
            RepeatPenalty = 1.1,
            PresencePenalty = 0.2,
            FrequencyPenalty = -0.1,
            RopeScaling = "linear",
            RopeScale = 0.5,
            RopeFreqBase = 10000,
            RopeFreqScale = 0.9,
            ContinuousBatching = "off",
            MmapMode = "off",
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
        Assert.Equal("4096", parsed.Flags["--batch-size"]);
        Assert.Equal("1024", parsed.Flags["--ubatch-size"]);
        Assert.Equal("4", parsed.Flags["--threads"]);
        Assert.Equal("true", parsed.Flags["--flash-attn"]);
        Assert.Equal("q8_0", parsed.Flags["--cache-type-k"]);
        Assert.Equal("q8_0", parsed.Flags["--cache-type-v"]);
        Assert.Equal("false", parsed.Flags["--kv-offload"]);
        Assert.Equal("false", parsed.Flags["--kv-unified"]);
        Assert.Equal("0.7", parsed.Flags["--temp"]);
        Assert.Equal("50", parsed.Flags["--top-k"]);
        Assert.Equal("0.9", parsed.Flags["--top-p"]);
        Assert.Equal("0.1", parsed.Flags["--min-p"]);
        Assert.Equal("512", parsed.Flags["--predict"]);
        Assert.Equal("42", parsed.Flags["--seed"]);
        Assert.Equal("32", parsed.Flags["--repeat-last-n"]);
        Assert.Equal("1.1", parsed.Flags["--repeat-penalty"]);
        Assert.Equal("0.2", parsed.Flags["--presence-penalty"]);
        Assert.Equal("-0.1", parsed.Flags["--frequency-penalty"]);
        Assert.Equal("linear", parsed.Flags["--rope-scaling"]);
        Assert.Equal("0.5", parsed.Flags["--rope-scale"]);
        Assert.Equal("10000", parsed.Flags["--rope-freq-base"]);
        Assert.Equal("0.9", parsed.Flags["--rope-freq-scale"]);
        Assert.Equal("false", parsed.Flags["--cont-batching"]);
        Assert.Equal("false", parsed.Flags["--mmap"]);
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
    public void ParseCommandRejectsApiKeyFile()
    {
        var parsed = LaunchCommandService.ParseCommand(@"--api-key-file C:\keys.txt");

        Assert.Empty(parsed.Errors);
        Assert.Contains(parsed.SecurityWarnings, warning => warning.Contains("--api-key-file", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("--api-key-file", parsed.Flags.Keys);
        Assert.DoesNotContain(@"C:\keys.txt", parsed.ExtraArgs);
    }

    [Fact]
    public void ParseCommandHandlesBooleanNegationAndValues()
    {
        var parsed = LaunchCommandService.ParseCommand("--no-mmap --kv-offload on --kv-offload off --kv-unified auto");

        Assert.Empty(parsed.Errors);
        Assert.Equal("false", parsed.Flags["--mmap"]);
        Assert.Equal("false", parsed.Flags["--kv-offload"]);
        Assert.Equal("true", parsed.Flags["--kv-unified"]);
    }

    [Fact]
    public void ParseCommandPreservesAutoBooleanDefaults()
    {
        var parsed = LaunchCommandService.ParseCommand("--flash-attn auto");

        Assert.Empty(parsed.Errors);
        Assert.Equal("auto", parsed.Flags["--flash-attn"]);
    }

    [Fact]
    public void ParseCommandInvertsExplicitValuesForNegatedBooleanFlags()
    {
        var parsed = LaunchCommandService.ParseCommand("--no-kv-offload on --no-kv-unified off");

        Assert.Empty(parsed.Errors);
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
    public void BuildCommandTokensReturnsRawUnquotedPaths()
    {
        const string modelPath = "C:\\Models\\my model.gguf";
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = modelPath,
            ContextSize = 4096
        };

        var tokens = LaunchCommandService.BuildCommandTokens(options);

        var modelIndex = tokens.ToList().IndexOf("--model");
        Assert.True(modelIndex >= 0);
        // Tokens feed ProcessStartInfo.ArgumentList, which applies OS-level quoting itself,
        // so the value must be the raw path with no added quotes or escaped backslashes.
        Assert.Equal(modelPath, tokens[modelIndex + 1]);
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
            Temperature = 0.7,
            FlagValues = new Dictionary<string, string> { ["--temp"] = "0.99" }
        };

        var command = LaunchCommandService.BuildCommand(options);
        var parsed = LaunchCommandService.ParseCommand(command);

        Assert.Equal("0.7", parsed.Flags["--temp"]);
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
        Assert.DoesNotContain(" --mmap ", $" {command} ");
    }

    [Fact]
    public void BuildCommandEmitsExplicitFirstClassBooleanWhenItMatchesSchemaDefault()
    {
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = _modelFile,
            MmapMode = "on"
        };

        var tokens = LaunchCommandService.BuildCommandTokens(options);

        Assert.Contains("--mmap", tokens);
        Assert.DoesNotContain("--no-mmap", tokens);
    }

    [Fact]
    public void BuildCommandEmitsZeroWhenContextCheckpointsAreOff()
    {
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = _modelFile,
            ContextCheckpointsMode = "off"
        };

        var tokens = LaunchCommandService.BuildCommandTokens(options).ToList();
        var index = tokens.IndexOf("--ctx-checkpoints");

        Assert.True(index >= 0);
        Assert.Equal("0", tokens[index + 1]);
        Assert.DoesNotContain("--checkpoint-min-step", tokens);
    }

    [Fact]
    public void ParseCommandRejectsContradictoryBooleanAliases()
    {
        var parsed = LaunchCommandService.ParseCommand("--mmap --no-mmap");

        Assert.Contains(parsed.Errors, error => error.Contains("Conflicting flags", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("true", parsed.Flags["--mmap"]);
    }

    [Fact]
    public void CanonicalizeFlagValuesMigratesLegacyNegativeAliases()
    {
        var canonical = LaunchCommandService.CanonicalizeFlagValues(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["--no-escape"] = "true"
            });

        Assert.Equal("false", canonical["--escape"]);
        Assert.DoesNotContain("--no-escape", canonical.Keys);
    }

    [Fact]
    public void CanonicalizeFlagValuesPrefersCurrentPositiveValueOverLegacyNegativeAlias()
    {
        var canonical = LaunchCommandService.CanonicalizeFlagValues(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["--escape"] = "true",
                ["--no-escape"] = "true"
            });

        Assert.Single(canonical);
        Assert.Equal("true", canonical["--escape"]);
    }

    [Fact]
    public void MultiTokenFlagsRequireExactArity()
    {
        var validation = LaunchCommandValidator.Validate(
            new Dictionary<string, string>
            {
                ["--control-vector-layer-range"] = "1 5 9"
            },
            validateFilePaths: false);
        var parsed = LaunchCommandService.ParseCommand("--control-vector-layer-range 1 5 9");

        Assert.False(validation.Ok);
        Assert.Contains(validation.Errors, error => error.Contains("exactly 2", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(parsed.Errors, error => error.Contains("exactly 2", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("--control-vector-layer-range", parsed.Flags.Keys);

        var options = new LlamaServerLaunchOptions
        {
            ModelPath = _modelFile,
            FlagValues = new Dictionary<string, string>
            {
                ["--control-vector-layer-range"] = "1 5 9"
            }
        };
        Assert.Throws<InvalidOperationException>(() => LaunchCommandService.BuildCommandTokens(options));
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
    public void BuildCommandAlwaysIncludesModelEvenWhenNotInSupportedFlags()
    {
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = _modelFile,
            ContextSize = 4096,
            Threads = 4,
            FlashAttention = "on",
            // Simulate a --help parse that produced a non-empty set missing the model flag.
            SupportedFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "--ctx-size" }
        };

        var command = LaunchCommandService.BuildCommand(options);

        Assert.Contains("--model", command);
        Assert.Contains("--ctx-size", command);
        Assert.DoesNotContain("--threads", command);
        Assert.DoesNotContain("--flash-attn", command);
    }

    [Fact]
    public void BuildCommandTokens_EmptySupportedFlags_DoesNotFilterModel()
    {
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = _modelFile,
            ContextSize = 4096,
            Threads = 4,
            FlashAttention = "on",
            SupportedFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };

        var tokens = LaunchCommandService.BuildCommandTokens(options);

        Assert.Contains("--model", tokens);
        Assert.Contains(_modelFile, tokens);
        Assert.Contains("--ctx-size", tokens);
        Assert.Contains("--threads", tokens);
        Assert.Contains("--flash-attn", tokens);
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

    [Theory]
    [InlineData("--threads ", "Flag '--threads' requires a value.")]
    [InlineData("--threads -1", "--threads must be at least 0.")]
    [InlineData("--unknown-flag", "--unknown-flag")]
    [InlineData("-", "-")]
    [InlineData("--", "--")]
    public void ParseCommand_ReportsUnknownOrMalformedFlags(string command, string expectedToken)
    {
        var parsed = LaunchCommandService.ParseCommand(command);

        if (command.Contains("threads", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains(expectedToken, parsed.Errors, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            Assert.Empty(parsed.Errors);
            Assert.Contains(expectedToken, parsed.ExtraArgs, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void IsDefaultFlagValue_HandlesNullDefaultBooleans()
    {
        var version = LlamaServerFlagSchema.FindByName("--version")!;
        var mlock = LlamaServerFlagSchema.FindByName("--mlock")!;

        Assert.True(LaunchCommandService.IsDefaultFlagValue(version, ""));
        Assert.True(LaunchCommandService.IsDefaultFlagValue(version, "off"));
        Assert.True(LaunchCommandService.IsDefaultFlagValue(version, "auto"));
        Assert.False(LaunchCommandService.IsDefaultFlagValue(version, "on"));
        Assert.False(LaunchCommandService.IsDefaultFlagValue(version, "true"));

        Assert.True(LaunchCommandService.IsDefaultFlagValue(mlock, "off"));
        Assert.True(LaunchCommandService.IsDefaultFlagValue(mlock, "false"));
        Assert.False(LaunchCommandService.IsDefaultFlagValue(mlock, "on"));
        Assert.False(LaunchCommandService.IsDefaultFlagValue(mlock, "true"));
    }

    [Fact]
    public void IsDefaultFlagValue_HandlesAutoBooleansAndNumericDefaults()
    {
        var flash = LlamaServerFlagSchema.FindByName("--flash-attn")!;
        var ctx = LlamaServerFlagSchema.FindByName("--ctx-size")!;
        var temp = LlamaServerFlagSchema.FindByName("--temp")!;

        Assert.True(LaunchCommandService.IsDefaultFlagValue(flash, "auto"));
        Assert.False(LaunchCommandService.IsDefaultFlagValue(flash, "off"));
        Assert.False(LaunchCommandService.IsDefaultFlagValue(flash, "on"));

        Assert.True(LaunchCommandService.IsDefaultFlagValue(ctx, "0"));
        Assert.False(LaunchCommandService.IsDefaultFlagValue(ctx, "4096"));

        Assert.True(LaunchCommandService.IsDefaultFlagValue(temp, "0.8"));
        Assert.False(LaunchCommandService.IsDefaultFlagValue(temp, "1.0"));
    }

    [Fact]
    public void BuildCommandTokens_PutsModelFirstAndFollowsFlagOrder()
    {
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = _modelFile,
            ContextSize = 4096,
            Threads = 8,
            FlashAttention = "on",
            FlagOrder = ["--threads", "--ctx-size", "--flash-attn"]
        };

        var tokens = LaunchCommandService.BuildCommandTokens(options).ToList();

        var modelIndex = tokens.IndexOf("--model");
        var threadsIndex = tokens.IndexOf("--threads");
        var ctxIndex = tokens.IndexOf("--ctx-size");
        var flashIndex = tokens.IndexOf("--flash-attn");

        Assert.True(modelIndex >= 0);
        Assert.True(threadsIndex > modelIndex);
        Assert.True(ctxIndex > threadsIndex);
        Assert.True(flashIndex > ctxIndex);
    }

    [Fact]
    public void BuildCommandTokens_FollowsFlagOrderForKnownFlags()
    {
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = _modelFile,
            ContextSize = 4096,
            Threads = 8,
            FlashAttention = "on",
            FlagOrder = ["--threads", "--ctx-size", "--flash-attn"]
        };

        var tokens = LaunchCommandService.BuildCommandTokens(options).ToList();

        var threadsIndex = tokens.IndexOf("--threads");
        var ctxIndex = tokens.IndexOf("--ctx-size");
        var flashIndex = tokens.IndexOf("--flash-attn");

        Assert.True(threadsIndex >= 0);
        Assert.True(ctxIndex > threadsIndex);
        Assert.True(flashIndex > ctxIndex);
    }

    [Fact]
    public void BuildCommandTokens_IncludesFlagNotInFlagOrder()
    {
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = _modelFile,
            ContextSize = 4096,
            Threads = 8,
            FlashAttention = "on",
            FlagOrder = ["--threads", "--ctx-size"]
        };

        var tokens = LaunchCommandService.BuildCommandTokens(options).ToList();

        Assert.Contains("--threads", tokens);
        Assert.Contains("--ctx-size", tokens);
        Assert.Contains("--flash-attn", tokens);
    }

    [Fact]
    public void BuildCommandTokens_OmitsFlagWhenResetToDefault()
    {
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = _modelFile,
            ContextSize = 4096,
            Threads = 8,
            FlagOrder = ["--threads", "--ctx-size"]
        };

        var tokens = LaunchCommandService.BuildCommandTokens(options).Select(t => t).ToList();
        Assert.Contains("--ctx-size", tokens);
        Assert.Contains("--threads", tokens);

        options = options with
        {
            ContextSize = 0,
            Threads = 0
        };

        tokens = LaunchCommandService.BuildCommandTokens(options).Select(t => t).ToList();
        // --ctx-size 0 means "load context size from the model", which differs from omitting
        // the flag (the server would pick its own default), so 0 must still be emitted.
        Assert.Contains("--ctx-size", tokens);
        Assert.Equal("0", tokens[tokens.IndexOf("--ctx-size") + 1]);
        Assert.DoesNotContain("--threads", tokens);
    }

    [Fact]
    public void BuildCommandTokens_EmitsGpuLayersZeroOnGpuBackend()
    {
        // --n-gpu-layers 0 forces CPU-only inference; omitting the flag would let the
        // server's own offload heuristics take over, so an explicit 0 must be emitted.
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = _modelFile,
            Backend = RuntimeBackend.Cuda,
            GpuLayers = 0
        };

        var tokens = LaunchCommandService.BuildCommandTokens(options).ToList();

        Assert.Contains("--n-gpu-layers", tokens);
        Assert.Equal("0", tokens[tokens.IndexOf("--n-gpu-layers") + 1]);
    }

    [Fact]
    public void BuildCommandTokens_EmitsFlashAttentionOnAsValuePair()
    {
        // --flash-attn is tri-state (on/off/auto) and requires an explicit value; a bare
        // token would make the server consume the next argument as the flag's value.
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = _modelFile,
            FlashAttention = "on",
            CacheTypeK = "q8_0"
        };

        var tokens = LaunchCommandService.BuildCommandTokens(options).ToList();

        var flashIndex = tokens.IndexOf("--flash-attn");
        Assert.True(flashIndex >= 0);
        Assert.Equal("on", tokens[flashIndex + 1]);
    }

    [Fact]
    public void BuildCommandTokens_ReportsDroppedUnsupportedFlags()
    {
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = _modelFile,
            ContextSize = 4096,
            MmapMode = "off",
            SupportedFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "--model", "--ctx-size", "--mmap"
            }
        };

        var dropped = new List<string>();
        var tokens = LaunchCommandService.BuildCommandTokens(options, dropped);

        // --no-mmap is not advertised and --mmap's default is not "auto", so the "off"
        // selection cannot be expressed; the drop must be reported, not silent.
        Assert.DoesNotContain("--no-mmap", tokens);
        Assert.Contains(dropped, f => f.Contains("mmap", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FilterExtraArgsForRuntime_DropsKnownUnsupportedFlagsAndKeepsUnknownTokens()
    {
        var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "--model", "--ctx-size" };
        var extraArgs = new List<string> { "--swa-full", "--top-k", "50", "--future-flag", "7" };

        var dropped = new List<string>();
        var filtered = LaunchCommandService.FilterExtraArgsForRuntime(extraArgs, supported, dropped);

        // Schema-known flags the runtime does not advertise are dropped with their values;
        // schema-unknown tokens are preserved because they may be valid for a newer build.
        Assert.DoesNotContain("--swa-full", filtered);
        Assert.DoesNotContain("--top-k", filtered);
        Assert.DoesNotContain("50", filtered);
        Assert.Contains("--future-flag", filtered);
        Assert.Contains("7", filtered);
        Assert.Contains("--swa-full", dropped);
        Assert.Contains("--top-k", dropped);
    }

    [Fact]
    public void BuildCommandTokens_DoesNotEmitAliasedFlagTwice()
    {
        // A first-class value keyed by an alias (--model-draft) and a user value stored
        // under the primary name (--spec-draft-model) are the same flag; only one may win.
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = _modelFile,
            SpeculativeType = "draft-simple",
            SpecDraftModelPath = "draft-a.gguf",
            FlagValues = new Dictionary<string, string> { ["--spec-draft-model"] = "draft-b.gguf" }
        };

        var tokens = LaunchCommandService.BuildCommandTokens(options).ToList();

        var draftTokens = tokens.Where(t =>
            string.Equals(t, "--model-draft", StringComparison.OrdinalIgnoreCase)
            || string.Equals(t, "--spec-draft-model", StringComparison.OrdinalIgnoreCase)
            || string.Equals(t, "-md", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Single(draftTokens);
    }
}
