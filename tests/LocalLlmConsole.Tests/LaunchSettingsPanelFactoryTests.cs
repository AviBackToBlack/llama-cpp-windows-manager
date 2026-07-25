using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LocalLlmConsole.Models;
using LocalLlmConsole.Services;
using LocalLlmConsole.ViewModels;

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
    public void SetValueByFlagName_UnmatchedEnumLeavesComboUnselected()
    {
        RunInSta(() =>
        {
            var combo = new ComboBox { ItemsSource = new[] { "auto", "none", "deepseek" } };
            combo.SelectedItem = "auto";
            var controls = new LaunchSettingsFormControls { ReasoningFormatCombo = combo };

            controls.SetValueByFlagName("--reasoning-format", "future-format");

            Assert.Null(combo.SelectedItem);
        });
    }

    [Fact]
    public void Read_InvalidPreviewDoesNotResetFormControls()
    {
        RunInSta(() =>
        {
            var controls = CreateFullControls();
            var defaults = AppSettings.CreateDefault("C:\\Workspace");
            LaunchSettingsFormBinder.Apply(controls, defaults);
            controls.ReasoningFormatCombo!.SelectedItem = "deepseek";
            controls.CommandPreviewBox!.Text = "--reasoning-format future-format";

            var error = Assert.Throws<InvalidOperationException>(
                () => LaunchSettingsFormBinder.Read(defaults, controls, parseCommandPreview: true));

            Assert.Contains("--reasoning-format", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("deepseek", controls.ReasoningFormatCombo.SelectedItem);
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
    public void SetValueByFlagName_ContextCheckpointsZero_SetsModeOffAndSavesWithoutError()
    {
        RunInSta(() =>
        {
            var controls = CreateFullControls();
            var defaults = AppSettings.CreateDefault("C:\\Workspace");
            LaunchSettingsFormBinder.Apply(controls, defaults);

            // The builder emits "--ctx-checkpoints 0" for checkpoints off; the round-trip must
            // map it back to off rather than an on-with-zero-count combo the validator rejects.
            controls.CommandPreviewBox!.Text = "--ctx-checkpoints 0";
            var settings = LaunchSettingsFormBinder.Read(defaults, controls, parseCommandPreview: true);

            Assert.Equal("off", controls.ContextCheckpointsCombo!.SelectedItem!.ToString());
            Assert.Equal("off", settings.ContextCheckpointsMode);
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

    [Fact]
    public void ControlFactory_NullDefaultBooleanCombo_DoesNotSelectOnByDefault()
    {
        RunInSta(() =>
        {
            var flag = LlamaServerFlagSchema.FindByName("--version")!;
            var control = LaunchSettingsControlFactory.CreateControl(flag, null);

            Assert.Equal(-1, ((ComboBox)control).SelectedIndex);
            Assert.True(string.IsNullOrWhiteSpace(LaunchSettingsControlFactory.GetControlValue(control)));
        });
    }

    [Fact]
    public void Read_GeneratedNullDefaultBoolean_DoesNotEmitUntilExplicitlyEnabled()
    {
        RunInSta(() =>
        {
            var controls = CreateFullControls();
            var versionFlag = LlamaServerFlagSchema.FindByName("--version")!;
            var control = LaunchSettingsControlFactory.CreateControl(versionFlag, null);
            controls.GeneratedControls["--version"] = control;

            var defaults = AppSettings.CreateDefault("C:\\Workspace");
            LaunchSettingsFormBinder.Apply(controls, defaults);

            var settings = LaunchSettingsFormBinder.Read(defaults, controls, parseCommandPreview: false);
            var command = LaunchSettingsFormBinder.BuildCommandPreview(settings, RuntimeBackend.Cpu);

            Assert.DoesNotContain("--version", command);
            Assert.DoesNotContain("--cache-list", command);
            Assert.DoesNotContain("--help", command);

            LaunchSettingsControlFactory.SetControlValue(control, "on");
            settings = LaunchSettingsFormBinder.Read(defaults, controls, parseCommandPreview: false);
            command = LaunchSettingsFormBinder.BuildCommandPreview(settings, RuntimeBackend.Cpu);

            Assert.Contains("--version", command);
        });
    }

    [Fact]
    public void BuildCommandPreview_FollowsFlagOrder()
    {
        RunInSta(() =>
        {
            var controls = CreateFullControls();
            controls.FlagOrder = ["--threads", "--ctx-size", "--flash-attn"];
            var defaults = AppSettings.CreateDefault("C:\\Workspace") with
            {
                ContextSize = 4096,
                Threads = 8,
                FlashAttention = "on"
            };

            var command = LaunchSettingsFormBinder.BuildCommandPreview(defaults, RuntimeBackend.Cpu, controls.FlagOrder);

            var threadsIndex = command.IndexOf("--threads", StringComparison.OrdinalIgnoreCase);
            var ctxIndex = command.IndexOf("--ctx-size", StringComparison.OrdinalIgnoreCase);
            var flashIndex = command.IndexOf("--flash-attn", StringComparison.OrdinalIgnoreCase);

            Assert.True(threadsIndex >= 0);
            Assert.True(ctxIndex > threadsIndex);
            Assert.True(flashIndex > ctxIndex);
        });
    }

    [Fact]
    public void ValidateCommandPreview_SetsRedBorderAndStatusOnError()
    {
        RunInSta(() =>
        {
            var controls = new LaunchSettingsFormControls { CommandPreviewBox = new TextBox { Text = "--threads " } };
            string? status = null;
            LaunchSettingsFormBinder.ValidateCommandPreview(controls, s => status = s);
            Assert.Equal(Brushes.Red, controls.CommandPreviewBox!.BorderBrush);
            Assert.Equal(LaunchInputState.Invalid, LaunchInputVisualState.GetState(controls.CommandPreviewBox));
            Assert.NotNull(status);
            Assert.Contains("requires a value", status, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void ValidateCommandPreview_SetsGreenBorderAndClearsStatusOnValid()
    {
        RunInSta(() =>
        {
            var controls = new LaunchSettingsFormControls { CommandPreviewBox = new TextBox { Text = "--threads 8" } };
            string? status = "initial";
            LaunchSettingsFormBinder.ValidateCommandPreview(controls, s => status = s);
            Assert.Equal(Brushes.Green, controls.CommandPreviewBox!.BorderBrush);
            Assert.Equal(LaunchInputState.Valid, LaunchInputVisualState.GetState(controls.CommandPreviewBox));
            Assert.Equal("", status);
        });
    }

    [Fact]
    public void ApplyFormDrivenCommandPreviewState_DoesNotPaintValidCommandRedForFormError()
    {
        RunInSta(() =>
        {
            var controls = new LaunchSettingsFormControls
            {
                CommandPreviewBox = new TextBox { Text = "--ctx-size 4096" }
            };
            string? status = null;

            LaunchSettingsFormBinder.ApplyFormDrivenCommandPreviewState(
                controls,
                ["Threads must be a whole number."],
                value => status = value);

            Assert.Equal(Brushes.Green, controls.CommandPreviewBox.BorderBrush);
            Assert.Equal(LaunchInputState.Valid, LaunchInputVisualState.GetState(controls.CommandPreviewBox));
            Assert.Equal("Threads must be a whole number.", status);
        });
    }

    [Fact]
    public void AttachChangeHandlers_CommandPreviewValidationUpdatesStateOnEachTextChange()
    {
        RunInSta(() =>
        {
            var controls = new LaunchSettingsFormControls { CommandPreviewBox = new TextBox() };
            LaunchSettingsFormBinder.AttachChangeHandlers(
                controls,
                () => { },
                (_, _) => { },
                validateCommandPreview: () => LaunchSettingsFormBinder.ValidateCommandPreview(controls));

            controls.CommandPreviewBox.Text = "--";

            Assert.Equal(Brushes.Red, controls.CommandPreviewBox.BorderBrush);
            Assert.Equal(LaunchInputState.Invalid, LaunchInputVisualState.GetState(controls.CommandPreviewBox));
        });
    }

    [Fact]
    public void UpdateFlagVisualStates_CombinesDefaultChangedAndInvalidColors()
    {
        RunInSta(() =>
        {
            var editor = new TextBox { Tag = "--threads", Text = "0" };
            var label = new TextBlock();
            var state = new LaunchSettingsPanelState();
            state.FormControls.ThreadsBox = editor;
            state.LaunchSettingElements["Threads"] = [label, editor];

            LaunchSettingsFormBinder.UpdateFlagVisualStates(state.FormControls, state);

            Assert.Equal(Brushes.Gray, label.Foreground);
            Assert.Equal(DependencyProperty.UnsetValue, editor.ReadLocalValue(System.Windows.Controls.Control.BorderBrushProperty));
            Assert.Equal(DependencyProperty.UnsetValue, editor.ReadLocalValue(System.Windows.Controls.Control.BorderThicknessProperty));

            editor.Text = "8";
            LaunchSettingsFormBinder.UpdateFlagVisualStates(state.FormControls, state);

            Assert.Equal(Brushes.Green, label.Foreground);
            Assert.Equal(Brushes.Green, editor.BorderBrush);
            Assert.Equal(new Thickness(1), editor.BorderThickness);

            editor.Text = "-1";
            LaunchSettingsFormBinder.UpdateFlagVisualStates(state.FormControls, state);

            Assert.Equal(Brushes.Red, label.Foreground);
            Assert.Equal(Brushes.Red, editor.BorderBrush);
            Assert.Equal(new Thickness(1), editor.BorderThickness);
        });
    }

    [Fact]
    public void UpdateFlagVisualStates_ValidatesGeneratedControls()
    {
        RunInSta(() =>
        {
            var flag = LlamaServerFlagSchema.FindByName("--threads-batch")!;
            var editor = Assert.IsType<TextBox>(LaunchSettingsControlFactory.CreateControl(flag, "-1"));
            var label = new TextBlock();
            var state = new LaunchSettingsPanelState();
            state.FormControls.GeneratedControls[flag.PrimaryName] = editor;
            state.LaunchSettingElements["Batch threads"] = [label, editor];

            LaunchSettingsFormBinder.UpdateFlagVisualStates(state.FormControls, state);

            Assert.Equal(Brushes.Red, label.Foreground);
            Assert.Equal(Brushes.Red, editor.BorderBrush);
        });
    }

    [Fact]
    public void UpdateFlagVisualStates_AllowsFirstClassUnsetSentinel()
    {
        RunInSta(() =>
        {
            var editor = new TextBox { Tag = "--spec-draft-p-split", Text = "-1" };
            var label = new TextBlock();
            var state = new LaunchSettingsPanelState();
            state.FormControls.SpecDraftPSplitBox = editor;
            state.LaunchSettingElements["Draft split"] = [label, editor];

            LaunchSettingsFormBinder.UpdateFlagVisualStates(state.FormControls, state);

            Assert.Equal(Brushes.Green, label.Foreground);
            Assert.Equal(Brushes.Green, editor.BorderBrush);
        });
    }

    [Fact]
    public void UpdateFlagVisualStates_ColorsVisiblePickerControl()
    {
        RunInSta(() =>
        {
            var valueBox = new TextBox { Text = @"C:\models\mtp.gguf", Visibility = Visibility.Collapsed };
            var pickerButton = new Button();
            var picker = new Grid { Tag = "--mtp-head" };
            picker.Children.Add(valueBox);
            picker.Children.Add(pickerButton);
            var label = new TextBlock();
            var state = new LaunchSettingsPanelState();
            state.FormControls.MtpHeadPathBox = valueBox;
            state.LaunchSettingElements["MTP head"] = [label, picker];

            LaunchSettingsFormBinder.UpdateFlagVisualStates(state.FormControls, state);

            Assert.Equal(Brushes.Green, label.Foreground);
            Assert.Equal(Brushes.Green, pickerButton.BorderBrush);
            Assert.Equal(DependencyProperty.UnsetValue, valueBox.ReadLocalValue(System.Windows.Controls.Control.BorderBrushProperty));
        });
    }

    [Fact]
    public void AttachChangeHandlers_TextChanged_InvokesUpdatePreview()
    {
        RunInSta(() =>
        {
            var controls = CreateFullControls();
            var changedCount = 0;
            var previewCount = 0;
            var commandPreviewChangedCount = 0;
            var validateCount = 0;

            LaunchSettingsFormBinder.AttachChangeHandlers(
                controls,
                () => changedCount++,
                (_, _) => { },
                () => commandPreviewChangedCount++,
                () => previewCount++,
                () => validateCount++);

            controls.ThreadsBox!.Text = "8";

            Assert.True(changedCount > 0);
            Assert.True(previewCount > 0);
            Assert.Equal(0, commandPreviewChangedCount);
            Assert.Equal(0, validateCount);
        });
    }

    [Fact]
    public void CreatePanel_DefaultRead_DoesNotIncludeSecurityCriticalFlagsAndValidates()
    {
        RunInSta(() =>
        {
            try
            {
                EnsureApplicationResources();

                var settings = AppSettings.CreateDefault("C:\\Workspace") with
                {
                    ModelApiKey = new string('a', 32)
                };

                var request = new LaunchSettingsPanelRequest(
                    settings,
                    new[] { new RuntimeChoice("cpu", "CPU", RuntimeBackend.Cpu) },
                    false,
                    () => { },
                    _ => { },
                    () => { },
                    () => Task.CompletedTask,
                    () => Task.CompletedTask,
                    () => { },
                    () => Task.CompletedTask,
                    () => Task.CompletedTask,
                    () => Task.CompletedTask,
                    () => { });

                var panel = LaunchSettingsPanelFactory.Create(request);
                var controls = panel.FormControls;
                Assert.NotNull(controls.PromptCacheCombo);
                LaunchSettingsFormBinder.Apply(controls, settings);
                // In a headless STA thread the ComboBox item collections can be lazily materialized;
                // ensure every combo has a selected item so Read/ReadControls produce valid values.
                foreach (var combo in controls.ComboBoxes)
                {
                    if (combo is null || combo.SelectedItem is not null) continue;
                    _ = combo.Items.Count;
                    if (combo.Items.Count > 0) combo.SelectedItem = combo.Items[0];
                }

                var readSettings = LaunchSettingsFormBinder.Read(settings, controls, parseCommandPreview: false);

                Assert.False(readSettings.FlagValues.ContainsKey("--host"));
                Assert.False(readSettings.FlagValues.ContainsKey("--port"));
                Assert.DoesNotContain(readSettings.FlagValues, kvp =>
                    LlamaServerFlagSchema.FindByName(kvp.Key)?.IsSecurityCritical == true);

                var launchRequest = new RuntimeLaunchRequest
                {
                    Mode = RuntimeMode.Native,
                    Backend = RuntimeBackend.Cpu,
                    ExecutablePath = "llama-server.exe",
                    ModelPath = "model.gguf",
                    Host = "127.0.0.1",
                    Port = readSettings.Port,
                    ApiKey = readSettings.ModelApiKey,
                    RequireApiKeyAuth = true,
                    ContextSize = readSettings.ContextSize,
                    GpuLayers = readSettings.GpuLayers,
                    ParallelSlots = readSettings.ParallelSlots,
                    BatchSize = readSettings.BatchSize,
                    MicroBatchSize = readSettings.MicroBatchSize,
                    Threads = readSettings.Threads,
                    FlashAttention = readSettings.FlashAttention,
                    CacheTypeK = readSettings.CacheTypeK,
                    CacheTypeV = readSettings.CacheTypeV,
                    KvOffload = readSettings.KvOffload,
                    KvUnified = readSettings.KvUnified,
                    PromptCacheMode = readSettings.PromptCacheMode,
                    PromptCacheRamMb = readSettings.PromptCacheRamMb,
                    ContextCheckpointsMode = readSettings.ContextCheckpointsMode,
                    ContextCheckpointCount = readSettings.ContextCheckpointCount,
                    ContextCheckpointEveryNTokens = readSettings.ContextCheckpointEveryNTokens,
                    ContinuousBatching = readSettings.ContinuousBatching,
                    ReasoningMode = readSettings.ReasoningMode,
                    ReasoningFormat = readSettings.ReasoningFormat,
                    ReasoningBudget = readSettings.ReasoningBudget,
                    VisionMode = readSettings.VisionMode,
                    VisionProjectorPath = readSettings.VisionProjectorPath,
                    VisionImageMinTokens = readSettings.VisionImageMinTokens,
                    VisionImageMaxTokens = readSettings.VisionImageMaxTokens,
                    JinjaMode = readSettings.JinjaMode,
                    MmapMode = readSettings.MmapMode,
                    MlockMode = readSettings.MlockMode,
                    Temperature = readSettings.Temperature,
                    TopK = readSettings.TopK,
                    TopP = readSettings.TopP,
                    MinP = readSettings.MinP,
                    MaxTokens = readSettings.MaxTokens,
                    Seed = readSettings.Seed,
                    RepeatLastN = readSettings.RepeatLastN,
                    RepeatPenalty = readSettings.RepeatPenalty,
                    PresencePenalty = readSettings.PresencePenalty,
                    FrequencyPenalty = readSettings.FrequencyPenalty,
                    RopeScaling = readSettings.RopeScaling,
                    RopeScale = readSettings.RopeScale,
                    RopeFreqBase = readSettings.RopeFreqBase,
                    RopeFreqScale = readSettings.RopeFreqScale,
                    SpeculativeType = readSettings.SpeculativeType,
                    SpecDraftModelPath = readSettings.SpecDraftModelPath,
                    MtpHeadPath = readSettings.MtpHeadPath,
                    SpecDraftGpuLayers = readSettings.SpecDraftGpuLayers,
                    SpecDraftMinTokens = readSettings.SpecDraftMinTokens,
                    SpecDraftMaxTokens = readSettings.SpecDraftMaxTokens,
                    SpecDraftPSplit = readSettings.SpecDraftPSplit,
                    SpecDraftPMin = readSettings.SpecDraftPMin,
                    SpecDraftCacheTypeK = readSettings.SpecDraftCacheTypeK,
                    SpecDraftCacheTypeV = readSettings.SpecDraftCacheTypeV,
                    FlagValues = readSettings.FlagValues,
                    ExtraArgs = Array.Empty<string>()
                };

                Assert.Equal("auto", readSettings.PromptCacheMode);
                var validation = RuntimeAdapter.Validate(launchRequest);
                Assert.True(validation.Ok, string.Join(" ", validation.Errors));
            }
            finally
            {
                ResetApplicationInstance();
            }
        });
    }

    private static void EnsureApplicationResources()
    {
        if (Application.Current is not null) return;

        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources = new ResourceDictionary
        {
            ["AppBack"] = Brushes.Black,
            ["PanelBack"] = Brushes.Black,
            ["PanelBackAlt"] = Brushes.Black,
            ["PanelBorder"] = Brushes.Black,
            ["PanelBorderStrong"] = Brushes.Black,
            ["ControlBack"] = Brushes.Black,
            ["ControlHover"] = Brushes.Black,
            ["ControlPressed"] = Brushes.Black,
            ["InputBack"] = Brushes.Black,
            ["ReadOnlyBack"] = Brushes.Black,
            ["GridRowBack"] = Brushes.Black,
            ["GridRowAlt"] = Brushes.Black,
            ["TextMain"] = Brushes.White,
            ["TextMuted"] = Brushes.Gray,
            ["TextSoft"] = Brushes.White,
            ["Accent"] = Brushes.Green,
            ["AccentStrong"] = Brushes.Green,
            ["AccentSoft"] = Brushes.Green,
            ["InfoSoft"] = Brushes.Black,
            ["Warning"] = Brushes.Yellow,
            ["DropDownPickerButton"] = new Style(typeof(Button))
        };
    }

    private static void ResetApplicationInstance()
    {
        var instanceField = typeof(Application).GetField("_appInstance", BindingFlags.NonPublic | BindingFlags.Static);
        var createdField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.NonPublic | BindingFlags.Static);
        instanceField?.SetValue(null, null);
        createdField?.SetValue(null, false);
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

    [Fact]
    public void CreatePanel_DoesNotGenerateModelControl()
    {
        RunInSta(() =>
        {
            try
            {
                EnsureApplicationResources();
                var settings = AppSettings.CreateDefault("C:\\Workspace");
                var request = new LaunchSettingsPanelRequest(
                    settings,
                    new[] { new RuntimeChoice("cpu", "CPU", RuntimeBackend.Cpu) },
                    false,
                    () => { },
                    _ => { },
                    () => { },
                    () => Task.CompletedTask,
                    () => Task.CompletedTask,
                    () => { },
                    () => Task.CompletedTask,
                    () => Task.CompletedTask,
                    () => Task.CompletedTask,
                    () => { });
                var panel = LaunchSettingsPanelFactory.Create(request);
                Assert.DoesNotContain(panel.FormControls.GeneratedControls, kvp =>
                    string.Equals(kvp.Key, "--model", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                ResetApplicationInstance();
            }
        });
    }

    [Fact]
    public void CreatePanel_DoesNotGenerateDuplicateControlsForFirstClassAliases()
    {
        RunInSta(() =>
        {
            try
            {
                EnsureApplicationResources();
                var settings = AppSettings.CreateDefault("C:\\Workspace");
                var request = new LaunchSettingsPanelRequest(
                    settings,
                    new[] { new RuntimeChoice("cpu", "CPU", RuntimeBackend.Cpu) },
                    false,
                    () => { },
                    _ => { },
                    () => { },
                    () => Task.CompletedTask,
                    () => Task.CompletedTask,
                    () => { },
                    () => Task.CompletedTask,
                    () => Task.CompletedTask,
                    () => Task.CompletedTask,
                    () => { });
                var panel = LaunchSettingsPanelFactory.Create(request);

                // The draft-model box registers under the alias --model-draft; exclusion must
                // cover every alias or the generated pass duplicates the flag's control.
                Assert.NotNull(panel.FormControls.SpecDraftModelPathBox);
                Assert.DoesNotContain(panel.FormControls.GeneratedControls, kvp =>
                    string.Equals(kvp.Key, "--spec-draft-model", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(kvp.Key, "--model-draft", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                ResetApplicationInstance();
            }
        });
    }
}
