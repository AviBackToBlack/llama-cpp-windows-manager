using LocalLlmConsole.Models;
using LocalLlmConsole.Services;

namespace LocalLlmConsole.Tests;

public sealed class RuntimeAdapterTests
{
    private static RuntimeLaunchRequest ValidRequest() => new()
    {
        Mode = RuntimeMode.Native,
        Backend = RuntimeBackend.Cpu,
        ExecutablePath = "llama-server.exe",
        ModelPath = "model.gguf",
        Host = "127.0.0.1",
        ApiKey = new string('a', 32),
        RequireApiKeyAuth = true,
        Port = 8081
    };

    [Fact]
    public void ValidatePassesForValidRequest()
    {
        var result = RuntimeAdapter.Validate(ValidRequest());

        Assert.True(result.Ok);
    }

    [Fact]
    public void BuildArgsIncludesHostPortAndModel()
    {
        var args = RuntimeAdapter.BuildArgs(ValidRequest());

        Assert.Contains("--host", args);
        Assert.Contains("127.0.0.1", args);
        Assert.Contains("--port", args);
        Assert.Contains("8081", args);
        Assert.Contains("--model", args);
        Assert.Contains("model.gguf", args);
    }

    [Fact]
    public void BuildArgsPreservesLiteralWindowsModelPath()
    {
        const string modelPath = @"C:\Models\my model.gguf";

        var args = RuntimeAdapter.BuildArgs(ValidRequest() with { ModelPath = modelPath });

        Assert.Contains(modelPath, args);
        Assert.DoesNotContain("\"C:\\\\Models\\\\my model.gguf\"", args);
    }

    [Fact]
    public void BuildArgsAppendsCustomExtraArgs()
    {
        var args = RuntimeAdapter.BuildArgs(ValidRequest() with
        {
            ExtraArgs = CustomLaunchParameterParser.Parse("--n-cpu-moe 999 --device-draft CUDA1")
        });

        Assert.Equal([
            "--n-cpu-moe",
            "999",
            "--device-draft",
            "CUDA1"
        ], args.TakeLast(4).ToArray());
    }

    [Fact]
    public void ValidateRejectsHostInExtraArgs()
    {
        var result = RuntimeAdapter.Validate(ValidRequest() with
        {
            ExtraArgs = CustomLaunchParameterParser.Parse("--host 0.0.0.0")
        });

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Contains("--host", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateRejectsPortAndApiKeyInExtraArgs()
    {
        var result = RuntimeAdapter.Validate(ValidRequest() with
        {
            ExtraArgs = CustomLaunchParameterParser.Parse("--port 9090 --api-key secret")
        });

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Contains("--port", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.Contains("--api-key", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateRejectsInvalidFlagValues()
    {
        var result = RuntimeAdapter.Validate(ValidRequest() with
        {
            FlagValues = new Dictionary<string, string> { ["--flash-attn"] = "banana" }
        });

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Contains("flash-attn", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildArgsRespectsFlagValues()
    {
        var args = RuntimeAdapter.BuildArgs(ValidRequest() with
        {
            FlagValues = new Dictionary<string, string> { ["--cpu-mask"] = "0xFF" }
        });

        Assert.Contains("--cpu-mask", args);
        Assert.Contains("0xFF", args);
    }

    [Fact]
    public void BuildArgsFirstClassWinsOverFlagValues()
    {
        var args = RuntimeAdapter.BuildArgs(ValidRequest() with
        {
            Temperature = 0.8,
            FlagValues = new Dictionary<string, string> { ["--temp"] = "0.99" }
        });

        var argsList = args.ToList();
        var tempIndex = argsList.IndexOf("--temp");
        Assert.True(tempIndex >= 0);
        Assert.Equal("0.8", argsList[tempIndex + 1]);
    }

    [Fact]
    public void BuildArgsRoundTripsDefaultSettings()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var request = ValidRequest() with
            {
                ModelPath = tempFile,
                ContextSize = 0,
                GpuLayers = 0,
                Threads = 0,
                MaxTokens = -1,
                Seed = -1,
                SpeculativeType = "none"
            };

            var args = RuntimeAdapter.BuildArgs(request);
            var command = string.Join(" ", args);
            var parsed = LaunchCommandService.ParseCommand(command);

            Assert.Empty(parsed.Errors);
            Assert.Equal(tempFile, parsed.Flags["--model"]);
            Assert.Equal("0", parsed.Flags["--ctx-size"]);
            Assert.Equal("1", parsed.Flags["--parallel"]);
            Assert.Equal("2048", parsed.Flags["--batch-size"]);
            Assert.Equal("512", parsed.Flags["--ubatch-size"]);
            Assert.Equal("0.8", parsed.Flags["--temp"]);
            Assert.Equal("40", parsed.Flags["--top-k"]);
            Assert.Equal("0.95", parsed.Flags["--top-p"]);
            Assert.Equal("0.05", parsed.Flags["--min-p"]);
            Assert.Equal("64", parsed.Flags["--repeat-last-n"]);
            Assert.Equal("1", parsed.Flags["--repeat-penalty"]);
            Assert.Equal("0", parsed.Flags["--presence-penalty"]);
            Assert.Equal("0", parsed.Flags["--frequency-penalty"]);
            Assert.Equal("f16", parsed.Flags["--cache-type-k"]);
            Assert.Equal("f16", parsed.Flags["--cache-type-v"]);
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    [Fact]
    public void BuildArgsProducesSingleMetricsTokenWhenEnabled()
    {
        var args = RuntimeAdapter.BuildArgs(ValidRequest() with { EnableMetrics = true });

        Assert.Equal(1, args.Count(a => a == "--metrics"));
    }

    [Fact]
    public void ValidateReportsDuplicateFlagWhenAliasesUsedInExtraArgs()
    {
        var result = RuntimeAdapter.Validate(ValidRequest() with
        {
            ExtraArgs = CustomLaunchParameterParser.Parse("--temp 0.5 --temperature 0.8")
        });

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate flag", StringComparison.OrdinalIgnoreCase));
    }
}
