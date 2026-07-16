using System.Collections.Generic;
using LocalLlmConsole.Services;

namespace LocalLlmConsole.Tests;

public sealed class LaunchCommandValidatorTests
{
    [Fact]
    public void ValidFlagsPass()
    {
        var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--ctx-size"] = "4096",
            ["--batch-size"] = "2048",
            ["--ubatch-size"] = "512",
            ["--flash-attn"] = "on",
            ["--temp"] = "0.8",
            ["--top-k"] = "40",
            ["--top-p"] = "0.9",
            ["--min-p"] = "0.05",
            ["--threads"] = "4"
        };

        var result = LaunchCommandValidator.Validate(flags);

        Assert.True(result.Ok);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ContextSizeShorthandIsNormalized()
    {
        var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--ctx-size"] = "4k"
        };

        var result = LaunchCommandValidator.Validate(flags);

        Assert.True(result.Ok);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void InvalidNumericValueFails()
    {
        var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--top-p"] = "2"
        };

        var result = LaunchCommandValidator.Validate(flags);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Contains("top-p", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("--host", "0.0.0.0")]
    [InlineData("--port", "9090")]
    [InlineData("--api-key", "secret")]
    public void SecurityCriticalFlagsFail(string flag, string value)
    {
        var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [flag] = value
        };

        var result = LaunchCommandValidator.Validate(flags);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Contains("Security-critical", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UnknownFlagFails()
    {
        var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--unknown-flag"] = "value"
        };

        var result = LaunchCommandValidator.Validate(flags);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Contains("Unknown flag", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UbatchSizeGreaterThanBatchSizeFails()
    {
        var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--batch-size"] = "512",
            ["--ubatch-size"] = "1024"
        };

        var result = LaunchCommandValidator.Validate(flags);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Contains("ubatch-size", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ImageMinGreaterThanImageMaxFails()
    {
        var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--image-min-tokens"] = "100",
            ["--image-max-tokens"] = "50"
        };

        var result = LaunchCommandValidator.Validate(flags);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Contains("image-min-tokens", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ContextSizeInvalidValueFails()
    {
        var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--ctx-size"] = "not-a-number"
        };

        var result = LaunchCommandValidator.Validate(flags);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Contains("ctx-size", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void InvalidEnumValueFails()
    {
        var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--flash-attn"] = "banana"
        };

        var result = LaunchCommandValidator.Validate(flags);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Contains("flash-attn", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IntOutOfRangeFails()
    {
        var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--top-k"] = "100001"
        };

        var result = LaunchCommandValidator.Validate(flags);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Contains("top-k", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DoubleOutOfRangeFails()
    {
        var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--top-p"] = "2"
        };

        var result = LaunchCommandValidator.Validate(flags);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Contains("top-p", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExistingFileAndPathPass()
    {
        var tempFile = Path.GetTempFileName();
        var tempDir = Path.GetTempPath();

        try
        {
            var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["--model"] = tempFile,
                ["--log-prompts-dir"] = tempDir
            };

            var result = LaunchCommandValidator.Validate(flags);

            Assert.True(result.Ok);
            Assert.Empty(result.Errors);
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    [Fact]
    public void MissingFileAndPathFail()
    {
        var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--model"] = "C:\\does-not-exist.gguf",
            ["--log-prompts-dir"] = "C:\\does-not-exist-dir"
        };

        var result = LaunchCommandValidator.Validate(flags);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Contains("--model", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.Contains("--log-prompts-dir", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EmptyCommaListValueFails()
    {
        var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--samplers"] = ""
        };

        var result = LaunchCommandValidator.Validate(flags);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Contains("samplers", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ContextSizeNotRestrictedToMultipleOf128()
    {
        var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--ctx-size"] = "4097"
        };

        var result = LaunchCommandValidator.Validate(flags);

        Assert.True(result.Ok);
        Assert.Empty(result.Errors);
        Assert.DoesNotContain(result.Errors, e => e.Contains("128", StringComparison.OrdinalIgnoreCase));
    }
}
