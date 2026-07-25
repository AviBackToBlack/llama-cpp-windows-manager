using System.Diagnostics;
using LocalLlmConsole.Models;
using LocalLlmConsole.Services;

namespace LocalLlmConsole.Tests;

public sealed class RuntimeFlagTests
{
    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "LocalLlmConsole.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    [Fact]
    public void SchemaFindByNameMatchesPrimaryName()
    {
        var flag = LlamaServerFlagSchema.FindByName("--model");
        Assert.NotNull(flag);
        Assert.Equal("--model", flag.PrimaryName);
        Assert.Equal("Model", flag.Category);
        Assert.Equal(FlagValueType.File, flag.ValueType);
    }

    [Fact]
    public void SchemaFindByNameMatchesShortName()
    {
        var flag = LlamaServerFlagSchema.FindByName("-c");
        Assert.NotNull(flag);
        Assert.Equal("--ctx-size", flag.PrimaryName);
    }

    [Fact]
    public void SchemaFindByNameIsCaseInsensitive()
    {
        var flag = LlamaServerFlagSchema.FindByName("--HOST");
        Assert.NotNull(flag);
        Assert.Equal("--host", flag.PrimaryName);
    }

    [Theory]
    [InlineData("on", "--flash-attn", "on")]
    [InlineData("off", "--flash-attn", "off")]
    [InlineData("auto", null, null)]
    public void BuildCommand_EmitsFlashAttentionBooleanCorrectly(string value, string? expectedToken, string? expectedValue)
    {
        var flag = LlamaServerFlagSchema.FindByName("--flash-attn");
        Assert.NotNull(flag);
        Assert.Equal(FlagValueType.Boolean, flag.ValueType);

        var options = new LlamaServerLaunchOptions
        {
            ModelPath = "model.gguf",
            FlashAttention = value
        };

        var command = LaunchCommandService.BuildCommand(options);
        var tokens = CustomLaunchParameterParser.Parse(command).ToList();

        if (expectedToken is not null)
        {
            var index = tokens.IndexOf(expectedToken);
            Assert.True(index >= 0);
            if (expectedValue is not null)
                Assert.Equal(expectedValue, tokens[index + 1]);
        }
        if (expectedToken is null)
        {
            Assert.DoesNotContain("--flash-attn", tokens);
            Assert.DoesNotContain("--no-flash-attn", tokens);
        }
    }

    [Fact]
    public void SchemaIsKnownFlagIsCaseInsensitive()
    {
        Assert.True(LlamaServerFlagSchema.IsKnownFlag("--Flash-Attn"));
        Assert.True(LlamaServerFlagSchema.IsKnownFlag("--No-Mmap"));
        Assert.False(LlamaServerFlagSchema.IsKnownFlag("--Flash-Attn-Invalid"));
    }

    [Fact]
    public void SchemaIsKnownFlagRecognizesFlagsWithNegatedPrefix()
    {
        Assert.True(LlamaServerFlagSchema.IsKnownFlag("--no-mmap"));
        Assert.True(LlamaServerFlagSchema.IsKnownFlag("--no-kv-offload"));
    }

    [Fact]
    public void SchemaIsKnownFlagRejectsNonFlagValues()
    {
        Assert.False(LlamaServerFlagSchema.IsKnownFlag("model.gguf"));
        Assert.False(LlamaServerFlagSchema.IsKnownFlag("--unknown-flag"));
    }

    [Fact]
    public void SchemaIsKnownFlagCaseInsensitiveForLongFlags()
    {
        Assert.True(LlamaServerFlagSchema.IsKnownFlag("--Flash-Attn"));
        Assert.True(LlamaServerFlagSchema.IsKnownFlag("--CTX-Size"));
        Assert.False(LlamaServerFlagSchema.IsKnownFlag("--bogus-flag"));
    }

    [Fact]
    public void SchemaIncludesSecurityCriticalFlags()
    {
        var host = LlamaServerFlagSchema.FindByName("--host");
        var port = LlamaServerFlagSchema.FindByName("--port");
        var apiKey = LlamaServerFlagSchema.FindByName("--api-key");
        var apiKeyFile = LlamaServerFlagSchema.FindByName("--api-key-file");

        Assert.NotNull(host);
        Assert.NotNull(port);
        Assert.NotNull(apiKey);
        Assert.NotNull(apiKeyFile);
        Assert.True(host.IsSecurityCritical);
        Assert.True(port.IsSecurityCritical);
        Assert.True(apiKey.IsSecurityCritical);
        Assert.True(apiKeyFile.IsSecurityCritical);
    }

    [Fact]
    public void ParserExtractsFlagsFromHelpOutput()
    {
        var help = """
            ----- common params -----
            -h,    --help, --usage         print usage and exit
            --version                       show version
            -t, --threads N                number of threads to use during generation
            --flash-attn [auto|on|off]     enable flash attention
            -v, --verbose                  verbose output
            -s, --seed N, --samplers SEQ   sampling parameters
            """;

        var flags = RuntimeFlagHelpParser.ParseSupportedFlags(help);

        Assert.Contains("-h", flags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--help", flags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--usage", flags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--version", flags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("-t", flags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--threads", flags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--flash-attn", flags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("-v", flags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--verbose", flags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("-s", flags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--seed", flags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--samplers", flags, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParserIgnoresValuesAndHeaders()
    {
        var help = """
            ----- common params -----
            -t, --threads N        number of threads (0 = auto)
            --temp TEMP            temperature (0.0 - 1.0)
            --top-k N              top-k sampling
            N 0 1.0 hello world
            """;

        var flags = RuntimeFlagHelpParser.ParseSupportedFlags(help);

        Assert.Contains("-t", flags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--threads", flags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--temp", flags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--top-k", flags, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("N", flags, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("TEMP", flags, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("hello", flags, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParserIgnoresFlagLikeTokensInDescriptions()
    {
        var help = """
            --threads N        number of threads; see --flash-attn for acceleration
              This continuation mentions --mmap in prose.
            --temp TEMP        sampling temperature
            """;

        var flags = RuntimeFlagHelpParser.ParseSupportedFlags(help);

        Assert.Contains("--threads", flags);
        Assert.Contains("--temp", flags);
        Assert.DoesNotContain("--flash-attn", flags);
        Assert.DoesNotContain("--mmap", flags);
    }

    [Fact]
    public void ParserReturnsEmptySetForEmptyHelp()
    {
        Assert.Empty(RuntimeFlagHelpParser.ParseSupportedFlags(null));
        Assert.Empty(RuntimeFlagHelpParser.ParseSupportedFlags(""));
        Assert.Empty(RuntimeFlagHelpParser.ParseSupportedFlags("   \n\n  "));
    }

    [Fact]
    public async Task RunnerBuildsNativeHelpCommand()
    {
        var psi = new ProcessStartInfo();
        var processRunner = new FakeProcessRunner((p) =>
        {
            psi = p;
            return new ProcessRunResult(0, "", "");
        });
        var runner = new RuntimeFlagHelpRunner(processRunner, () => "wsl.exe");

        await runner.RunHelpAsync("C:\\runtimes\\llama-server.exe", RuntimeMode.Native, null, CancellationToken.None);

        Assert.Equal("C:\\runtimes\\llama-server.exe", psi.FileName);
        Assert.Contains("--help", psi.ArgumentList);
        Assert.Equal("C:\\runtimes", psi.WorkingDirectory);
    }

    [Fact]
    public async Task RunnerBuildsWslHelpCommand()
    {
        var psi = new ProcessStartInfo();
        var processRunner = new FakeProcessRunner((p) =>
        {
            psi = p;
            return new ProcessRunResult(0, "", "");
        });
        var runner = new RuntimeFlagHelpRunner(processRunner, () => "C:\\Windows\\System32\\wsl.exe");

        await runner.RunHelpAsync("C:\\runtimes\\llama-server", RuntimeMode.Wsl, "Ubuntu-24.04", CancellationToken.None);

        Assert.Equal("C:\\Windows\\System32\\wsl.exe", psi.FileName);
        Assert.Equal("-d", psi.ArgumentList[0]);
        Assert.Equal("Ubuntu-24.04", psi.ArgumentList[1]);
        Assert.Equal("--", psi.ArgumentList[2]);
        Assert.Equal("bash", psi.ArgumentList[3]);
        Assert.Equal("-lc", psi.ArgumentList[4]);

        var command = psi.ArgumentList[5];
        Assert.Contains("--help", command, StringComparison.Ordinal);
        Assert.Contains("/mnt/c/runtimes/llama-server", command, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CapabilityServiceReturnsSupportedAndUnknownFlags()
    {
        var help = """
            -h, --help
            --version
            --model FNAME
            --unknown-xyz VALUE
            """;
        var runner = new FakeHelpRunner(help);
        var service = new RuntimeFlagCapabilityService(runner, CreateTempRoot());

        var result = await service.GetCapabilitiesAsync("llama-server", RuntimeMode.Native, null, CancellationToken.None);

        Assert.Contains("--help", result.Supported, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("-h", result.Supported, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--version", result.Supported, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--model", result.Supported, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--unknown-xyz", result.Unknown, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("--unknown-xyz", result.Supported, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CapabilityServiceReportsUnsupportedSchemaFlags()
    {
        var help = """
            -h, --help
            --version
            """;
        var runner = new FakeHelpRunner(help);
        var service = new RuntimeFlagCapabilityService(runner, CreateTempRoot());

        var result = await service.GetCapabilitiesAsync("llama-server", RuntimeMode.Native, null, CancellationToken.None);

        Assert.Contains("--model", result.Unsupported, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--threads", result.Unsupported, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--flash-attn", result.Unsupported, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CapabilityServiceCachesResultInMemory()
    {
        var runner = new FakeHelpRunner("-h, --help\n--version");
        var service = new RuntimeFlagCapabilityService(runner, CreateTempRoot());

        var first = await service.GetCapabilitiesAsync("llama-server", RuntimeMode.Native, null, CancellationToken.None);
        var second = await service.GetCapabilitiesAsync("llama-server", RuntimeMode.Native, null, CancellationToken.None);

        Assert.Equal(1, runner.InvocationCount);
        Assert.Equal(first.Supported, second.Supported);
    }

    [Fact]
    public async Task CapabilityServiceWritesAndReadsFileCache()
    {
        var root = CreateTempRoot();
        var help = "-h, --help\n--version";
        var runner = new FakeHelpRunner(help);
        var service = new RuntimeFlagCapabilityService(runner, root, TimeSpan.FromMinutes(5));
        var executable = Path.Combine(root, "llama-server");
        File.WriteAllText(executable, "fake");

        var first = await service.GetCapabilitiesAsync(executable, RuntimeMode.Native, null, CancellationToken.None);

        var secondRunner = new FakeHelpRunner(help, throwIfCalled: true);
        var secondService = new RuntimeFlagCapabilityService(secondRunner, root, TimeSpan.FromMinutes(5));
        var second = await secondService.GetCapabilitiesAsync(executable, RuntimeMode.Native, null, CancellationToken.None);

        Assert.Single(Directory.GetFiles(root, "llama-server-help-*.json"));
        Assert.False(Directory.Exists(Path.Combine(root, "cache")));
        Assert.Equal(first.Supported, second.Supported);
        Assert.Equal(1, runner.InvocationCount);
        Assert.Equal(0, secondRunner.InvocationCount);
    }

    [Fact]
    public void SchemaFlagsHaveUniqueNamesAndPrimaryNames()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var flag in LlamaServerFlagSchema.All)
        {
            Assert.StartsWith("--", flag.PrimaryName);
            Assert.False(string.IsNullOrWhiteSpace(flag.Description));
            Assert.False(string.IsNullOrWhiteSpace(flag.Category));
            Assert.False(string.IsNullOrWhiteSpace(flag.UiLabel));

            foreach (var name in flag.Names)
            {
                Assert.False(string.IsNullOrWhiteSpace(name));
                Assert.DoesNotContain(name, seen);
                seen.Add(name);
            }
        }
    }

    [Fact]
    public void FindByNameMatchesEveryNameInSchema()
    {
        foreach (var flag in LlamaServerFlagSchema.All)
        {
            foreach (var name in flag.Names)
            {
                var found = LlamaServerFlagSchema.FindByName(name);
                Assert.NotNull(found);
                Assert.Equal(flag.PrimaryName, found.PrimaryName);
                Assert.True(LlamaServerFlagSchema.IsKnownFlag(name));

                if (flag.ValueType == FlagValueType.Boolean && name.StartsWith("--", StringComparison.Ordinal))
                    Assert.True(LlamaServerFlagSchema.IsKnownFlag("--no-" + name[2..]));
            }
        }
    }

    [Fact]
    public void ParserHandlesEmptyAndMalformedHelp()
    {
        Assert.Empty(RuntimeFlagHelpParser.ParseSupportedFlags(null));
        Assert.Empty(RuntimeFlagHelpParser.ParseSupportedFlags("   "));

        var malformed = RuntimeFlagHelpParser.ParseSupportedFlags("!!! --- not flags --- 123 1.0 value --bad? --?bad");
        Assert.Empty(malformed);

        var mixed = RuntimeFlagHelpParser.ParseSupportedFlags("""
            ----- common params -----
            -h, --help, --usage            print usage and exit
            N 123 TEMP value
            --threads N                    number of threads
            --bad? --?bad
            --flash-attn [auto|on|off]     enable flash attention
            """);

        Assert.Contains("-h", mixed, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--help", mixed, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--usage", mixed, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--threads", mixed, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--flash-attn", mixed, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("N", mixed, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("TEMP", mixed, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("123", mixed, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("value", mixed, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunnerParsesRealLlamaServerHelp()
    {
        var executable = @"C:\AI\llama.cpp\llama-server.exe";
        Assert.SkipUnless(File.Exists(executable), "llama-server binary not found at C:\\AI\\llama.cpp.");

        var runner = new RuntimeFlagHelpRunner(new TrackedProcessRunner(), () => "wsl.exe");
        var result = await runner.RunHelpAsync(executable, RuntimeMode.Native, null, TestContext.Current.CancellationToken);

        Assert.Equal(0, result.ExitCode);

        var flags = RuntimeFlagHelpParser.ParseSupportedFlags(result.Output);
        Assert.Contains("--help", flags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--model", flags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--ctx-size", flags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("--flash-attn", flags, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CapabilityServiceForwardsWslDistroToRunner()
    {
        var runner = new CapturingHelpRunner("-h, --help\n--version");
        var service = new RuntimeFlagCapabilityService(runner, CreateTempRoot());

        await service.GetCapabilitiesAsync("llama-server", RuntimeMode.Wsl, "Ubuntu-24.04", CancellationToken.None);
        Assert.Equal(RuntimeMode.Wsl, runner.LastMode);
        Assert.Equal("Ubuntu-24.04", runner.LastWslDistro);

        await service.GetCapabilitiesAsync("llama-server", RuntimeMode.Native, null, CancellationToken.None);
        Assert.Equal(RuntimeMode.Native, runner.LastMode);
        Assert.Null(runner.LastWslDistro);
    }

    [Fact]
    public async Task CapabilityServiceCachesByModeAndWslDistro()
    {
        var runner = new CapturingHelpRunner("-h, --help\n--version");
        var service = new RuntimeFlagCapabilityService(runner, CreateTempRoot());

        await service.GetCapabilitiesAsync("llama-server", RuntimeMode.Native, null, CancellationToken.None);
        await service.GetCapabilitiesAsync("llama-server", RuntimeMode.Wsl, "Ubuntu-22.04", CancellationToken.None);
        await service.GetCapabilitiesAsync("llama-server", RuntimeMode.Wsl, "Ubuntu-24.04", CancellationToken.None);

        Assert.Equal(3, runner.InvocationCount);
    }

    private sealed class FakeProcessRunner : IProcessRunner
    {
        private readonly Func<ProcessStartInfo, ProcessRunResult> _handler;

        public FakeProcessRunner(Func<ProcessStartInfo, ProcessRunResult> handler) => _handler = handler;

        public Task<ProcessRunResult> RunAsync(ProcessStartInfo psi, TimeSpan timeout, CancellationToken cancellationToken = default, string? standardInput = null)
            => Task.FromResult(_handler(psi));
    }

    private sealed class FakeHelpRunner : IRuntimeFlagHelpRunner
    {
        private readonly string _helpText;
        private readonly bool _throwIfCalled;

        public FakeHelpRunner(string helpText, bool throwIfCalled = false)
        {
            _helpText = helpText;
            _throwIfCalled = throwIfCalled;
        }

        public int InvocationCount { get; private set; }

        public Task<ProcessRunResult> RunHelpAsync(string executablePath, RuntimeMode mode, string? wslDistro, CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            if (_throwIfCalled)
                throw new InvalidOperationException("Runner should not be invoked when cache is available.");
            return Task.FromResult(new ProcessRunResult(0, _helpText, ""));
        }
    }

    private sealed class CapturingHelpRunner : IRuntimeFlagHelpRunner
    {
        private readonly string _helpText;

        public CapturingHelpRunner(string helpText) => _helpText = helpText;

        public int InvocationCount { get; private set; }

        public RuntimeMode LastMode { get; private set; }

        public string? LastWslDistro { get; private set; }

        public Task<ProcessRunResult> RunHelpAsync(string executablePath, RuntimeMode mode, string? wslDistro, CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            LastMode = mode;
            LastWslDistro = wslDistro;
            return Task.FromResult(new ProcessRunResult(0, _helpText, ""));
        }
    }

    // Phase 13 regression tests

    [Fact]
    public void ParseCommand_AcceptsDotsInFlagNames()
    {
        var parsed = LaunchCommandService.ParseCommand("--model model.gguf --fim-qwen-1.5b-default");

        Assert.Empty(parsed.Errors);
        Assert.Equal("true", parsed.Flags["--fim-qwen-1.5b-default"]);
    }

    [Fact]
    public void RuntimeFlagHelpParser_AcceptsDotsInFlagNames()
    {
        var flags = RuntimeFlagHelpParser.ParseSupportedFlags("--fim-qwen-1.5b-default\n--flash-attn");

        Assert.Contains("--fim-qwen-1.5b-default", flags);
        Assert.Contains("--flash-attn", flags);
    }

    [Fact]
    public void RuntimeSupportsToken_IsCaseSensitiveForShortAndInsensitiveForLong()
    {
        var supported = new HashSet<string>(StringComparer.Ordinal) { "-c", "--flash-attn" };

        Assert.True(LaunchCommandService.RuntimeSupportsToken("-c", supported));
        Assert.False(LaunchCommandService.RuntimeSupportsToken("-C", supported));
        Assert.True(LaunchCommandService.RuntimeSupportsToken("--Flash-Attn", supported));
        Assert.False(LaunchCommandService.RuntimeSupportsToken("--Flash-Attn-Invalid", supported));
    }

    [Fact]
    public void BuildCommand_EmitsMultiTokenFlag()
    {
        var options = new LlamaServerLaunchOptions
        {
            ModelPath = "model.gguf",
            FlagValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["--control-vector-layer-range"] = "1 5"
            }
        };

        var tokens = LaunchCommandService.BuildCommandTokens(options).ToList();

        var idx = tokens.IndexOf("--control-vector-layer-range");
        Assert.True(idx >= 0, "Expected --control-vector-layer-range token");
        Assert.Equal("1", tokens[idx + 1]);
        Assert.Equal("5", tokens[idx + 2]);
    }

    [Fact]
    public void WslConvertsPathListAndScaledPathListComponentByComponent()
    {
        var request = new RuntimeLaunchRequest
        {
            Mode = RuntimeMode.Wsl,
            Backend = RuntimeBackend.Cpu,
            ExecutablePath = "llama-server",
            ModelPath = "model.gguf",
            FlagValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["--lora"] = "C:\\models\\a.gguf,C:\\models\\b.gguf",
                ["--control-vector-scaled"] = "C:\\cv\\c.gguf:0.75"
            }
        };

        var converted = LlamaProcessSupervisor.ConvertFlagValuesForWsl(request);

        Assert.Equal("/mnt/c/models/a.gguf,/mnt/c/models/b.gguf", converted.FlagValues["--lora"]);
        Assert.Equal("/mnt/c/cv/c.gguf:0.75", converted.FlagValues["--control-vector-scaled"]);
    }

    [Fact]
    public void NegatedPairInferenceDoesNotConflateUnrelatedHostFlags()
    {
        var host = LlamaServerFlagSchema.FindByName("--host");
        var noHost = LlamaServerFlagSchema.FindByName("--no-host");

        Assert.NotNull(host);
        Assert.NotNull(noHost);
        Assert.Equal(FlagValueType.String, host.ValueType);
        Assert.Equal(FlagValueType.Boolean, noHost.ValueType);
        Assert.Null(host.NegatedForm);
        Assert.Null(noHost.NegatedForm);

        var parsed = LaunchCommandService.ParseCommand("--no-host");
        Assert.Empty(parsed.Errors);
        Assert.Equal("true", parsed.Flags["--no-host"]);
    }

    [Fact]
    public void WslPathListsTrimSeparatorWhitespaceAndPreserveSpacesInsidePaths()
    {
        var request = new RuntimeLaunchRequest
        {
            Mode = RuntimeMode.Wsl,
            Backend = RuntimeBackend.Cpu,
            ExecutablePath = "llama-server",
            ModelPath = "model.gguf",
            FlagValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["--lora"] = "C:\\models\\first model.gguf, C:\\models\\second model.gguf",
                ["--lora-scaled"] = "C:\\models\\first model.gguf:0.5, C:\\models\\second model.gguf:1"
            }
        };

        var converted = LlamaProcessSupervisor.ConvertFlagValuesForWsl(request);

        Assert.Equal(
            "/mnt/c/models/first model.gguf,/mnt/c/models/second model.gguf",
            converted.FlagValues["--lora"]);
        Assert.Equal(
            "/mnt/c/models/first model.gguf:0.5,/mnt/c/models/second model.gguf:1",
            converted.FlagValues["--lora-scaled"]);
    }
}
