using System.Collections.Immutable;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using LocalLlmConsole.Models;
using LocalLlmConsole.Services;

namespace LocalLlmConsole.Tests;

public sealed class LaunchSettingsPanelFactoryTests
{
    [Fact]
    public void BuildCommandPreview_IncludesFirstClassAndGeneratedFlags()
    {
        var settings = AppSettings.CreateDefault("C:\\Workspace") with
        {
            Port = 8081,
            ContextSize = 4096,
            GpuLayers = 50,
            MaxTokens = 256,
            FlashAttention = "on",
            FlagValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["--verbose"] = "true" }.ToImmutableDictionary()
        };

        var command = LaunchSettingsFormBinder.BuildCommandPreview(settings, RuntimeBackend.Cuda);

        Assert.Contains("--ctx-size 4096", command);
        Assert.Contains("--n-gpu-layers 50", command);
        Assert.Contains("--flash-attn", command);
        Assert.Contains("--predict 256", command);
        Assert.Contains("--verbose", command);
    }

    [Fact]
    public void BuildCommandPreview_RoundTripsWithParseCommand()
    {
        var settings = AppSettings.CreateDefault("C:\\Workspace") with
        {
            Port = 8081,
            ContextSize = 4096,
            GpuLayers = 50,
            MaxTokens = 256,
            FlashAttention = "on"
        };

        var command = LaunchSettingsFormBinder.BuildCommandPreview(settings, RuntimeBackend.Cuda);
        var parsed = LaunchCommandService.ParseCommand(command);

        Assert.Equal("4096", parsed.Flags["--ctx-size"]);
        Assert.Equal("50", parsed.Flags["--n-gpu-layers"]);
        Assert.Equal("256", parsed.Flags["--predict"]);
        Assert.Empty(parsed.Errors);
        Assert.Empty(parsed.SecurityWarnings);
    }

    [Fact]
    public void ParseCommand_ParsesCommandPreviewAndMergesFlags()
    {
        var command = "--ctx-size 8192 --n-gpu-layers 33 --predict 128";

        var parsed = LaunchCommandService.ParseCommand(command);

        Assert.Equal("8192", parsed.Flags["--ctx-size"]);
        Assert.Equal("33", parsed.Flags["--n-gpu-layers"]);
        Assert.Equal("128", parsed.Flags["--predict"]);
    }

    [Fact]
    public void FlagSchema_UiLabel_DerivesFromLongFlagName()
    {
        var flag = new LlamaServerFlag(["--verbose", "-v"], "Logging", FlagValueType.Boolean, Description: "Verbose.");

        Assert.Equal("verbose", flag.UiLabel);
    }

    [Fact]
    public void MetadataService_Tooltip_ForGeneratedFlagIncludesPrimaryNameAndDescription()
    {
        var flag = new LlamaServerFlag(["--verbose"], "Logging", FlagValueType.Boolean, Description: "Verbose logging.");

        var tooltip = LaunchSettingMetadataService.Tooltip(flag);

        Assert.Contains("--verbose", tooltip);
        Assert.Contains("Verbose logging", tooltip);
    }

    [Fact]
    public void MetadataService_Tooltip_ForCommandLineLabelReturnsCommandLineHelp()
    {
        var tooltip = LaunchSettingMetadataService.Tooltip("Command line");

        Assert.Contains("command", tooltip, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LaunchOptions_FromAppSettings_IncludesCustomFlagValues()
    {
        var settings = AppSettings.CreateDefault("C:\\Workspace") with
        {
            FlagValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["--verbose"] = "true" }.ToImmutableDictionary()
        };

        var options = LaunchSettingsFormBinder.BuildCommandPreview(settings);

        Assert.Contains("--verbose", options);
    }

    [Fact]
    public void ControlFactory_CreatesControlForEverySchemaFlag()
    {
        RunInSta(() =>
        {
            foreach (var flag in LlamaServerFlagSchema.All)
            {
                var control = LaunchSettingsControlFactory.CreateControl(flag, null);
                Assert.NotNull(control);
                Assert.Equal(flag.PrimaryName, control.Tag);

                var testValue = flag.ValueType switch
                {
                    FlagValueType.Boolean => "on",
                    FlagValueType.Enum => flag.AllowedValues?.FirstOrDefault() ?? "on",
                    FlagValueType.Int or FlagValueType.Double => "123",
                    FlagValueType.File or FlagValueType.Path => "C:\\test.gguf",
                    _ => "test"
                };

                LaunchSettingsControlFactory.SetControlValue(control, testValue);
                var readValue = LaunchSettingsControlFactory.GetControlValue(control);
                Assert.False(string.IsNullOrWhiteSpace(readValue));
                Assert.Equal(testValue, readValue);
            }
        });
    }

    [Fact]
    public void ControlFactory_ProducesCorrectEditorTypeForEachValueType()
    {
        RunInSta(() =>
        {
            foreach (var flag in LlamaServerFlagSchema.All)
            {
                var control = LaunchSettingsControlFactory.CreateControl(flag, null);
                var editor = LaunchSettingsControlFactory.FindEditor(control);

                if (flag.ValueType is FlagValueType.Boolean or FlagValueType.Enum)
                {
                    Assert.IsAssignableFrom<ComboBox>(editor);
                }
                else
                {
                    Assert.IsAssignableFrom<TextBox>(editor);
                }
            }
        });
    }

    [Theory]
    [InlineData(RuntimeBackend.Cpu, false)]
    [InlineData(RuntimeBackend.Cuda, true)]
    public void BuildCommandPreview_RespectsBackendForGpuLayers(RuntimeBackend backend, bool shouldIncludeGpuLayers)
    {
        var settings = AppSettings.CreateDefault("C:\\Workspace") with { GpuLayers = 50 };

        var command = LaunchSettingsFormBinder.BuildCommandPreview(settings, backend);

        if (shouldIncludeGpuLayers)
            Assert.Contains("--n-gpu-layers 50", command);
        else
            Assert.DoesNotContain("--n-gpu-layers", command);
    }

    [Fact]
    public void SetValueByFlagName_CacheRam_SetsModeOnAndRamValue()
    {
        RunInSta(() =>
        {
            var controls = new LaunchSettingsFormControls
            {
                PromptCacheRamMbBox = new TextBox(),
                PromptCacheCombo = new ComboBox { ItemsSource = new[] { "auto", "on", "off" } }
            };

            var parsed = LaunchCommandService.ParseCommand("--cache-ram 8192");
            controls.SetValueByFlagName("--cache-ram", parsed.Flags["--cache-ram"]);

            Assert.Equal("8192", controls.PromptCacheRamMbBox!.Text);
            Assert.Equal("on", controls.PromptCacheCombo!.SelectedItem!.ToString());

            var settings = AppSettings.CreateDefault("C:\\Workspace") with
            {
                PromptCacheMode = "on",
                PromptCacheRamMb = 8192
            };
            var command = LaunchSettingsFormBinder.BuildCommandPreview(settings, RuntimeBackend.Cpu);
            Assert.Contains("--cache-ram 8192", command);
        });
    }

    [Fact]
    public void SetValueByFlagName_ContextCheckpoints_SetsModeOnAndCount()
    {
        RunInSta(() =>
        {
            var controls = new LaunchSettingsFormControls
            {
                ContextCheckpointCountBox = new TextBox(),
                ContextCheckpointEveryNTokensBox = new TextBox(),
                ContextCheckpointsCombo = new ComboBox { ItemsSource = new[] { "auto", "on", "off" } }
            };

            var parsed = LaunchCommandService.ParseCommand("--ctx-checkpoints 32 --checkpoint-min-step 256");
            controls.SetValueByFlagName("--ctx-checkpoints", parsed.Flags["--ctx-checkpoints"]);
            controls.SetValueByFlagName("--checkpoint-min-step", parsed.Flags["--checkpoint-min-step"]);

            Assert.Equal("32", controls.ContextCheckpointCountBox!.Text);
            Assert.Equal("256", controls.ContextCheckpointEveryNTokensBox!.Text);
            Assert.Equal("on", controls.ContextCheckpointsCombo!.SelectedItem!.ToString());

            var settings = AppSettings.CreateDefault("C:\\Workspace") with
            {
                ContextCheckpointsMode = "on",
                ContextCheckpointCount = 32,
                ContextCheckpointEveryNTokens = 256
            };
            var command = LaunchSettingsFormBinder.BuildCommandPreview(settings, RuntimeBackend.Cpu);
            Assert.Contains("--ctx-checkpoints 32", command);
            Assert.Contains("--checkpoint-min-step 256", command);
        });
    }

    private static void RunInSta(Action action)
    {
        var tcs = new TaskCompletionSource<object?>();
        var thread = new Thread(() =>
        {
            try
            {
                action();
                tcs.SetResult(null);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        tcs.Task.GetAwaiter().GetResult();
    }
}
