using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using LocalLlmConsole.Models;

namespace LocalLlmConsole.Tests;

public sealed class LaunchSettingsFormBinderTests
{
    [Fact]
    public void TryReadForPreview_ReturnsErrorsAndBuildableSettings()
    {
        RunInSta(() =>
        {
            var controls = CreateFullControls();
            var defaults = AppSettings.CreateDefault("C:\\Workspace");
            controls.ThreadsBox!.Text = "abc";

            var (settings, errors) = LaunchSettingsFormBinder.TryReadForPreview(defaults, controls);

            Assert.Contains(errors, e => e.Contains("Threads", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(defaults.Threads, settings.Threads);
            var preview = LaunchSettingsFormBinder.BuildCommandPreview(settings, RuntimeBackend.Cpu, controls.FlagOrder);
            Assert.DoesNotContain("--threads", preview);
        });
    }

    [Fact]
    public void UpdateCommandPreview_RemovesStaleFlagOnInvalidInput()
    {
        RunInSta(() =>
        {
            var controls = CreateFullControls();
            var defaults = AppSettings.CreateDefault("C:\\Workspace");
            LaunchSettingsFormBinder.Apply(controls, defaults);

            controls.ThreadsBox!.Text = "3";
            var (validSettings, _) = LaunchSettingsFormBinder.TryReadForPreview(defaults, controls);
            var validPreview = LaunchSettingsFormBinder.BuildCommandPreview(validSettings, RuntimeBackend.Cpu, controls.FlagOrder);
            Assert.Contains("--threads 3", validPreview);

            controls.ThreadsBox.Text = "";
            var (emptySettings, errors) = LaunchSettingsFormBinder.TryReadForPreview(defaults, controls, treatEmptyAsDefault: true);
            var emptyPreview = LaunchSettingsFormBinder.BuildCommandPreview(emptySettings, RuntimeBackend.Cpu, controls.FlagOrder);

            Assert.Empty(errors);
            Assert.DoesNotContain("--threads", emptyPreview);
        });
    }

    [Fact]
    public void ValidateCommandPreview_DoesNotClearStatusOnProgrammaticUpdate()
    {
        RunInSta(() =>
        {
            var controls = new LaunchSettingsFormControls { CommandPreviewBox = new TextBox { Text = "--threads 8" } };
            const string expected = "Unsupported flags moved to CustomParameters.";
            string? status = expected;

            LaunchSettingsFormBinder.ValidateCommandPreview(controls, s => status = s, isUserEdit: false);

            Assert.Equal(expected, status);
            Assert.Equal(Brushes.Green, controls.CommandPreviewBox!.BorderBrush);
        });
    }

    [Fact]
    public void PreviewReadCanonicalizesLegacyNegativeAliasValues()
    {
        RunInSta(() =>
        {
            var controls = CreateFullControls();
            controls.GeneratedControls["--escape"] = new ComboBox
            {
                ItemsSource = new[] { "auto", "on", "off" },
                SelectedItem = "on"
            };
            var settings = AppSettings.CreateDefault("C:\\Workspace") with
            {
                FlagValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["--no-escape"] = "true"
                }
            };

            LaunchSettingsFormBinder.Apply(controls, settings);
            var escapeControl = Assert.IsType<ComboBox>(controls.GeneratedControls["--escape"]);
            Assert.Equal("off", escapeControl.SelectedItem);
            escapeControl.SelectedItem = "on";

            var (read, errors) = LaunchSettingsFormBinder.TryReadForPreview(settings, controls);

            Assert.Empty(errors);
            Assert.Equal("true", read.FlagValues["--escape"]);
            Assert.DoesNotContain("--no-escape", read.FlagValues.Keys);
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
