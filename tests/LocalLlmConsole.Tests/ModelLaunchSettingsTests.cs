using LocalLlmConsole.Models;

namespace LocalLlmConsole.Tests;

public sealed class ModelLaunchSettingsTests
{
    [Fact]
    public void Sanitize_PreservesMtpAlias()
    {
        var appSettings = AppSettings.CreateDefault("C:\\Workspace") with { SpeculativeType = "mtp" };

        var model = ModelLaunchSettings.FromAppSettings(appSettings);

        Assert.Equal("atomic-mtp", model.SpeculativeType);
    }
}
