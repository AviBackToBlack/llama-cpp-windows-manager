using LocalLlmConsole.Services;

namespace LocalLlmConsole.Tests;

public sealed class LaunchSettingParserTests
{
    [Fact]
    public void TryReadInt_ReturnsDefaultAndNoErrorForEmptyText()
    {
        var result = LaunchSettingParser.TryReadInt("", "Threads", 0, null, out var value, out var error);

        Assert.True(result);
        Assert.Equal(0, value);
        Assert.Null(error);
    }

    [Fact]
    public void TryReadInt_ReturnsDefaultAndErrorForInvalidText()
    {
        var result = LaunchSettingParser.TryReadInt("abc", "Threads", 0, null, out var value, out var error);

        Assert.False(result);
        Assert.Equal(0, value);
        Assert.NotNull(error);
        Assert.Contains("whole number", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryReadDouble_ReturnsDefaultAndNoErrorForEmptyText()
    {
        var result = LaunchSettingParser.TryReadDouble("", "Temperature", 0, null, out var value, out var error);

        Assert.True(result);
        Assert.Equal(0.0, value);
        Assert.Null(error);
    }

    [Fact]
    public void TryReadDouble_ReturnsDefaultAndErrorForInvalidText()
    {
        var result = LaunchSettingParser.TryReadDouble("abc", "Temperature", 0, null, out var value, out var error);

        Assert.False(result);
        Assert.Equal(0.0, value);
        Assert.NotNull(error);
        Assert.Contains("number", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryReadContextSize_ReturnsDefaultAndNoErrorForEmptyText()
    {
        var result = LaunchSettingParser.TryReadContextSize("", out var value, out var error);

        Assert.True(result);
        Assert.Equal(0, value);
        Assert.Null(error);
    }

    [Fact]
    public void TryReadContextSize_ReturnsDefaultAndErrorForInvalidText()
    {
        var result = LaunchSettingParser.TryReadContextSize("abc", out var value, out var error);

        Assert.False(result);
        Assert.Equal(0, value);
        Assert.NotNull(error);
        Assert.Contains("Context size", error, StringComparison.OrdinalIgnoreCase);
    }
}
