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

        Assert.Equal("Verbose", flag.UiLabel);
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

            var parsed = LaunchCommandService.ParseCommand("--cache-ram 4096");
            controls.SetValueByFlagName("--cache-ram", parsed.Flags["--cache-ram"]);

            Assert.Equal("4096", controls.PromptCacheRamMbBox!.Text);
            Assert.Equal("on", controls.PromptCacheCombo!.SelectedItem!.ToString());

            var settings = AppSettings.CreateDefault("C:\\Workspace") with
            {
                PromptCacheMode = "on",
                PromptCacheRamMb = 4096
            };
            var command = LaunchSettingsFormBinder.BuildCommandPreview(settings, RuntimeBackend.Cpu);
            Assert.Contains("--cache-ram 4096", command);
        });
    }

    [Theory]
    [InlineData("--flash-attn", "on")]
    [InlineData("--mlock", "on")]
    public void Read_BooleanOnOffCombos_SurviveCommandPreviewRoundTrip(string flag, string mode)
    {
        RunInSta(() =>
        {
            var controls = CreateFullControls();
            var defaults = AppSettings.CreateDefault("C:\\Workspace");
            LaunchSettingsFormBinder.Apply(controls, defaults);

            // ParseCommand emits boolean flags as true/false; the combo round-trip must map
            // those back to on/off rather than silently reverting to the first combo item.
            controls.CommandPreviewBox!.Text = flag;
            LaunchSettingsFormBinder.Read(defaults, controls, parseCommandPreview: true);

            var combo = string.Equals(flag, "--flash-attn", StringComparison.Ordinal)
                ? controls.FlashAttentionCombo!
                : controls.MlockCombo!;
            Assert.Equal(mode, combo.SelectedItem!.ToString());
        });
    }

    [Fact]
    public void SetValueByFlagName_CacheRamZero_SetsModeOffAndSavesWithoutError()
    {
        RunInSta(() =>
        {
            var controls = CreateFullControls();
            var defaults = AppSettings.CreateDefault("C:\\Workspace");
            LaunchSettingsFormBinder.Apply(controls, defaults);

            // The builder emits "--cache-ram 0" for prompt-cache off; the round-trip must
            // map it back to off rather than an on-with-zero-RAM combo the validator rejects.
            controls.CommandPreviewBox!.Text = "--cache-ram 0";
            var settings = LaunchSettingsFormBinder.Read(defaults, controls, parseCommandPreview: true);

            Assert.Equal("off", controls.PromptCacheCombo!.SelectedItem!.ToString());
            Assert.Equal("off", settings.PromptCacheMode);
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

    [Fact]
    public void ParseAndMergeCommandPreview_ResetsRemovedFlagsToDefaults()
    {
        RunInSta(() =>
        {
            var controls = CreateFullControls();
            var defaults = AppSettings.CreateDefault("C:\\Workspace");
            LaunchSettingsFormBinder.Apply(controls, defaults);

            controls.CommandPreviewBox!.Text = "--ctx-size 2048";
            var settings = LaunchSettingsFormBinder.Read(defaults, controls, parseCommandPreview: true);
            var preview = LaunchSettingsFormBinder.BuildCommandPreview(settings, RuntimeBackend.Cpu);

            Assert.Contains("--ctx-size 2048", preview);
            Assert.DoesNotContain("--batch-size", preview);
            Assert.DoesNotContain("--temp", preview);
            Assert.DoesNotContain("--top-k", preview);
            Assert.DoesNotContain("--cache-type-k", preview);
        });
    }

    [Fact]
    public void SetValueByFlagName_NoMmproj_SelectsVisionOff()
    {
        RunInSta(() =>
        {
            var controls = new LaunchSettingsFormControls
            {
                VisionCombo = new ComboBox { ItemsSource = new[] { "auto", "on", "off" } }
            };

            var parsed = LaunchCommandService.ParseCommand("--no-mmproj true");
            controls.SetValueByFlagName("--no-mmproj", parsed.Flags["--no-mmproj"]);

            Assert.Equal("off", controls.VisionCombo!.SelectedItem!.ToString());
        });
    }

    [Fact]
    public void SetValueByFlagName_MmprojAuto_SetsVisionComboCorrectly()
    {
        RunInSta(() =>
        {
            var controls = new LaunchSettingsFormControls
            {
                VisionCombo = new ComboBox { ItemsSource = new[] { "auto", "on", "off" } }
            };

            controls.SetValueByFlagName("--mmproj-auto", "true");
            Assert.Equal("auto", controls.VisionCombo!.SelectedItem!.ToString());

            controls.SetValueByFlagName("--mmproj-auto", "false");
            Assert.Equal("off", controls.VisionCombo!.SelectedItem!.ToString());
        });
    }

    [Fact]
    public void Read_ShortFormAliases_RoundTripToFirstClassFields()
    {
        RunInSta(() =>
        {
            var controls = CreateFullControls();
            var defaults = AppSettings.CreateDefault("C:\\Workspace");
            LaunchSettingsFormBinder.Apply(controls, defaults);

            // ParseCommand keys these by the short alias; the form must still populate the
            // long-name-mapped first-class fields instead of dropping the pasted values.
            controls.CommandPreviewBox!.Text = "-ngl 50 -c 2048 -t 8 -b 4096";
            LaunchSettingsFormBinder.Read(defaults, controls, parseCommandPreview: true);

            Assert.Equal("50", controls.GpuLayersBox!.Text);
            Assert.Equal("2048", controls.ContextSizeBox!.Text);
            Assert.Equal("8", controls.ThreadsBox!.Text);
            Assert.Equal("4096", controls.BatchSizeBox!.Text);
        });
    }

    [Fact]
    public void Read_WithParseCommandPreviewFalse_DoesNotMutateControls()
    {
        RunInSta(() =>
        {
            var controls = CreateFullControls();
            var defaults = AppSettings.CreateDefault("C:\\Workspace");
            LaunchSettingsFormBinder.Apply(controls, defaults);

            controls.ContextSizeBox!.Text = "4096";
            controls.BatchSizeBox!.Text = "1024";
            controls.CommandPreviewBox!.Text = "--ctx-size 2048";
            var temperatureBefore = controls.TemperatureBox!.Text;

            var settings = LaunchSettingsFormBinder.Read(defaults, controls, parseCommandPreview: false);

            Assert.Equal("4096", controls.ContextSizeBox.Text);
            Assert.Equal("1024", controls.BatchSizeBox.Text);
            Assert.Equal(temperatureBefore, controls.TemperatureBox.Text);
            Assert.Equal(4096, settings.ContextSize);
        });
    }

    private static LaunchSettingsFormControls CreateFullControls()
    {
        var comboOptions = new[] { "auto", "on", "off", "none", "f16", "q8_0", "linear", "yarn", "deepseek", "deepseek-legacy" };
        return new LaunchSettingsFormControls
        {
            LaunchPortBox = new TextBox(),
            ContextSizeBox = new TextBox(),
            GpuLayersBox = new TextBox(),
            ParallelSlotsBox = new TextBox(),
            BatchSizeBox = new TextBox(),
            MicroBatchSizeBox = new TextBox(),
            ThreadsBox = new TextBox(),
            ReasoningBudgetBox = new TextBox(),
            VisionProjectorPathBox = new TextBox(),
            VisionImageMinTokensBox = new TextBox(),
            VisionImageMaxTokensBox = new TextBox(),
            TemperatureBox = new TextBox(),
            TopKBox = new TextBox(),
            TopPBox = new TextBox(),
            MinPBox = new TextBox(),
            MaxTokensBox = new TextBox(),
            SeedBox = new TextBox(),
            RepeatLastNBox = new TextBox(),
            RepeatPenaltyBox = new TextBox(),
            PresencePenaltyBox = new TextBox(),
            FrequencyPenaltyBox = new TextBox(),
            RopeScaleBox = new TextBox(),
            RopeFreqBaseBox = new TextBox(),
            RopeFreqScaleBox = new TextBox(),
            SpecDraftModelPathBox = new TextBox(),
            MtpHeadPathBox = new TextBox(),
            SpecDraftGpuLayersBox = new TextBox(),
            SpecDraftMinTokensBox = new TextBox(),
            SpecDraftMaxTokensBox = new TextBox(),
            SpecDraftPSplitBox = new TextBox(),
            SpecDraftPMinBox = new TextBox(),
            PromptCacheRamMbBox = new TextBox(),
            ContextCheckpointCountBox = new TextBox(),
            ContextCheckpointEveryNTokensBox = new TextBox(),
            CustomParametersBox = new TextBox(),
            CommandPreviewBox = new TextBox(),
            MetricsCombo = new ComboBox { ItemsSource = comboOptions },
            ReasoningCombo = new ComboBox { ItemsSource = comboOptions },
            ReasoningFormatCombo = new ComboBox { ItemsSource = comboOptions },
            VisionCombo = new ComboBox { ItemsSource = comboOptions },
            FlashAttentionCombo = new ComboBox { ItemsSource = comboOptions },
            CacheTypeKCombo = new ComboBox { ItemsSource = comboOptions },
            CacheTypeVCombo = new ComboBox { ItemsSource = comboOptions },
            KvOffloadCombo = new ComboBox { ItemsSource = comboOptions },
            KvUnifiedCombo = new ComboBox { ItemsSource = comboOptions },
            PromptCacheCombo = new ComboBox { ItemsSource = comboOptions },
            ContextCheckpointsCombo = new ComboBox { ItemsSource = comboOptions },
            ContinuousBatchingCombo = new ComboBox { ItemsSource = comboOptions },
            JinjaCombo = new ComboBox { ItemsSource = comboOptions },
            MmapCombo = new ComboBox { ItemsSource = comboOptions },
            MlockCombo = new ComboBox { ItemsSource = comboOptions },
            RopeScalingCombo = new ComboBox { ItemsSource = comboOptions },
            SpeculativeTypeCombo = new ComboBox { ItemsSource = comboOptions },
            SpecDraftCacheTypeKCombo = new ComboBox { ItemsSource = comboOptions },
            SpecDraftCacheTypeVCombo = new ComboBox { ItemsSource = comboOptions }
        };
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
