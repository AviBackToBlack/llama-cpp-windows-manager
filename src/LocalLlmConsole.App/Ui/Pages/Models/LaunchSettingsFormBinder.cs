using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace LocalLlmConsole;

/// <summary>Strongly-typed references to the launch settings WPF controls.</summary>
public sealed class LaunchSettingsFormControls
{
    public WpfTextBox? LaunchPortBox { get; set; }
    public WpfTextBox? ContextSizeBox { get; set; }
    public WpfTextBox? GpuLayersBox { get; set; }
    public WpfTextBox? ParallelSlotsBox { get; set; }
    public WpfTextBox? BatchSizeBox { get; set; }
    public WpfTextBox? MicroBatchSizeBox { get; set; }
    public WpfTextBox? ThreadsBox { get; set; }
    public WpfTextBox? ReasoningBudgetBox { get; set; }
    public WpfTextBox? VisionProjectorPathBox { get; set; }
    public WpfButton? VisionProjectorButton { get; set; }
    public WpfTextBox? VisionImageMinTokensBox { get; set; }
    public WpfTextBox? VisionImageMaxTokensBox { get; set; }
    public WpfTextBox? TemperatureBox { get; set; }
    public WpfTextBox? TopKBox { get; set; }
    public WpfTextBox? TopPBox { get; set; }
    public WpfTextBox? MinPBox { get; set; }
    public WpfTextBox? MaxTokensBox { get; set; }
    public WpfTextBox? SeedBox { get; set; }
    public WpfTextBox? RepeatLastNBox { get; set; }
    public WpfTextBox? RepeatPenaltyBox { get; set; }
    public WpfTextBox? PresencePenaltyBox { get; set; }
    public WpfTextBox? FrequencyPenaltyBox { get; set; }
    public WpfTextBox? RopeScaleBox { get; set; }
    public WpfTextBox? RopeFreqBaseBox { get; set; }
    public WpfTextBox? RopeFreqScaleBox { get; set; }
    public WpfTextBox? SpecDraftModelPathBox { get; set; }
    public WpfTextBox? MtpHeadPathBox { get; set; }
    public WpfButton? MtpHeadButton { get; set; }
    public WpfTextBox? SpecDraftGpuLayersBox { get; set; }
    public WpfTextBox? SpecDraftMinTokensBox { get; set; }
    public WpfTextBox? SpecDraftMaxTokensBox { get; set; }
    public WpfTextBox? SpecDraftPSplitBox { get; set; }
    public WpfTextBox? SpecDraftPMinBox { get; set; }
    public WpfTextBox? CustomParametersBox { get; set; }
    public WpfTextBox? CommandPreviewBox { get; set; }
    public WpfComboBox? RuntimeCombo { get; set; }

    public WpfComboBox? MetricsCombo { get; set; }
    public WpfComboBox? ReasoningCombo { get; set; }
    public WpfComboBox? ReasoningFormatCombo { get; set; }
    public WpfComboBox? VisionCombo { get; set; }
    public WpfComboBox? FlashAttentionCombo { get; set; }
    public WpfComboBox? CacheTypeKCombo { get; set; }
    public WpfComboBox? CacheTypeVCombo { get; set; }
    public WpfComboBox? KvOffloadCombo { get; set; }
    public WpfComboBox? KvUnifiedCombo { get; set; }
    public WpfComboBox? PromptCacheCombo { get; set; }
    public WpfTextBox? PromptCacheRamMbBox { get; set; }
    public WpfComboBox? ContextCheckpointsCombo { get; set; }
    public WpfTextBox? ContextCheckpointCountBox { get; set; }
    public WpfTextBox? ContextCheckpointEveryNTokensBox { get; set; }
    public WpfComboBox? ContinuousBatchingCombo { get; set; }
    public WpfComboBox? JinjaCombo { get; set; }
    public WpfComboBox? MmapCombo { get; set; }
    public WpfComboBox? MlockCombo { get; set; }
    public WpfComboBox? RopeScalingCombo { get; set; }
    public WpfComboBox? SpeculativeTypeCombo { get; set; }
    public WpfComboBox? SpecDraftCacheTypeKCombo { get; set; }
    public WpfComboBox? SpecDraftCacheTypeVCombo { get; set; }

    public Dictionary<string, FrameworkElement> GeneratedControls { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<WpfTextBox?> TextBoxes =>
    [
        LaunchPortBox, ContextSizeBox, GpuLayersBox, ParallelSlotsBox, BatchSizeBox, MicroBatchSizeBox,
        ThreadsBox, ReasoningBudgetBox, VisionProjectorPathBox, VisionImageMinTokensBox, VisionImageMaxTokensBox,
        TemperatureBox, TopKBox, TopPBox, MinPBox, MaxTokensBox, SeedBox, RepeatLastNBox,
        RepeatPenaltyBox, PresencePenaltyBox, FrequencyPenaltyBox, RopeScaleBox, RopeFreqBaseBox,
        RopeFreqScaleBox, SpecDraftModelPathBox, MtpHeadPathBox, SpecDraftGpuLayersBox, SpecDraftMinTokensBox,
        SpecDraftMaxTokensBox, SpecDraftPSplitBox, SpecDraftPMinBox, PromptCacheRamMbBox,
        ContextCheckpointCountBox, ContextCheckpointEveryNTokensBox, CustomParametersBox
    ];

    public IEnumerable<WpfComboBox?> ComboBoxes =>
    [
        MetricsCombo, ReasoningCombo, ReasoningFormatCombo, VisionCombo, FlashAttentionCombo,
        CacheTypeKCombo, CacheTypeVCombo, KvOffloadCombo, KvUnifiedCombo, PromptCacheCombo,
        ContextCheckpointsCombo, ContinuousBatchingCombo, JinjaCombo, MmapCombo, MlockCombo, RopeScalingCombo, SpeculativeTypeCombo,
        SpecDraftCacheTypeKCombo, SpecDraftCacheTypeVCombo
    ];

    public bool TryGetValueByFlagName(string flagName, out string value)
    {
        value = "";
        var firstClass = GetFirstClassControlByFlagName(flagName);
        if (firstClass is not null)
        {
            if (firstClass is WpfTextBox textBox)
            {
                value = textBox.Text.Trim();
                return true;
            }
            if (firstClass is WpfComboBox combo)
            {
                value = (combo.SelectedItem?.ToString() ?? combo.Text ?? "").Trim().ToLowerInvariant();
                return true;
            }
        }

        var primary = LocalLlmConsole.Services.LlamaServerFlagSchema.FindByName(flagName)?.PrimaryName ?? flagName;
        if (GeneratedControls.TryGetValue(primary, out var generatedControl))
        {
            var generatedValue = LaunchSettingsControlFactory.GetControlValue(generatedControl);
            if (generatedValue is not null)
            {
                value = generatedValue;
                return true;
            }
        }

        return false;
    }

    public FrameworkElement? GetFirstClassControlByFlagName(string flagName)
    {
        var map = new Dictionary<string, FrameworkElement?>(StringComparer.OrdinalIgnoreCase)
        {
            ["--ctx-size"] = ContextSizeBox,
            ["--threads"] = ThreadsBox,
            ["--gpu-layers"] = GpuLayersBox,
            ["--n-gpu-layers"] = GpuLayersBox,
            ["--batch-size"] = BatchSizeBox,
            ["--ubatch-size"] = MicroBatchSizeBox,
            ["--flash-attn"] = FlashAttentionCombo,
            ["--cache-type-k"] = CacheTypeKCombo,
            ["--cache-type-v"] = CacheTypeVCombo,
            ["--kv-offload"] = KvOffloadCombo,
            ["--kv-unified"] = KvUnifiedCombo,
            ["--cache-ram-mode"] = PromptCacheCombo,
            ["--cache-ram"] = PromptCacheRamMbBox,
            ["--ctx-checkpoints-mode"] = ContextCheckpointsCombo,
            ["--ctx-checkpoints"] = ContextCheckpointCountBox,
            ["--checkpoint-min-step"] = ContextCheckpointEveryNTokensBox,
            ["--cont-batching"] = ContinuousBatchingCombo,
            ["--mmap"] = MmapCombo,
            ["--mlock"] = MlockCombo,
            ["--reasoning"] = ReasoningCombo,
            ["--reasoning-format"] = ReasoningFormatCombo,
            ["--reasoning-budget"] = ReasoningBudgetBox,
            ["--jinja"] = JinjaCombo,
            ["--mmproj-auto"] = VisionCombo,
            ["--no-mmproj"] = VisionCombo,
            ["--mmproj"] = VisionProjectorPathBox,
            ["--image-min-tokens"] = VisionImageMinTokensBox,
            ["--image-max-tokens"] = VisionImageMaxTokensBox,
            ["--temp"] = TemperatureBox,
            ["--top-k"] = TopKBox,
            ["--top-p"] = TopPBox,
            ["--min-p"] = MinPBox,
            ["--predict"] = MaxTokensBox,
            ["--n-predict"] = MaxTokensBox,
            ["--seed"] = SeedBox,
            ["--repeat-last-n"] = RepeatLastNBox,
            ["--repeat-penalty"] = RepeatPenaltyBox,
            ["--presence-penalty"] = PresencePenaltyBox,
            ["--frequency-penalty"] = FrequencyPenaltyBox,
            ["--rope-scaling"] = RopeScalingCombo,
            ["--rope-scale"] = RopeScaleBox,
            ["--rope-freq-base"] = RopeFreqBaseBox,
            ["--rope-freq-scale"] = RopeFreqScaleBox,
            ["--spec-type"] = SpeculativeTypeCombo,
            ["--model-draft"] = SpecDraftModelPathBox,
            ["--spec-draft-model"] = SpecDraftModelPathBox,
            ["--mtp-head"] = MtpHeadPathBox,
            ["--cache-type-k-draft"] = SpecDraftCacheTypeKCombo,
            ["--cache-type-v-draft"] = SpecDraftCacheTypeVCombo,
            ["--spec-draft-n-max"] = SpecDraftMaxTokensBox,
            ["--spec-draft-n-min"] = SpecDraftMinTokensBox,
            ["--spec-draft-ngl"] = SpecDraftGpuLayersBox,
            ["--gpu-layers-draft"] = SpecDraftGpuLayersBox,
            ["--n-gpu-layers-draft"] = SpecDraftGpuLayersBox,
            ["--spec-draft-p-split"] = SpecDraftPSplitBox,
            ["--spec-draft-p-min"] = SpecDraftPMinBox,
            ["--parallel"] = ParallelSlotsBox,
            ["--metrics"] = MetricsCombo,
            ["--custom-params"] = CustomParametersBox
        };
        if (map.TryGetValue(flagName, out var control)) return control;

        // ParseCommand keys flags by the exact alias typed (e.g. "-ngl", "-c"), so a pasted
        // short-form flag would miss the long-name map. Fall back to the flag's other names
        // to keep first-class fields in sync instead of silently dropping the value.
        var schemaFlag = LocalLlmConsole.Services.LlamaServerFlagSchema.FindByName(flagName);
        if (schemaFlag is not null)
        {
            foreach (var name in schemaFlag.Names)
                if (map.TryGetValue(name, out var aliasControl)) return aliasControl;
        }

        return null;
    }

    public void SetValueByFlagName(string flagName, string value)
    {
        if (string.Equals(flagName, "--cache-ram", StringComparison.OrdinalIgnoreCase))
        {
            SetTextBox(PromptCacheRamMbBox, value);
            // The builder emits "--cache-ram 0" for the off mode, so a zero value must map
            // back to off; forcing "on" here produces an on-with-zero-RAM combo the validator rejects.
            SetComboValue(PromptCacheCombo, string.Equals(value.Trim(), "0", StringComparison.Ordinal) ? "off" : "on");
            return;
        }

        if (string.Equals(flagName, "--ctx-checkpoints", StringComparison.OrdinalIgnoreCase))
        {
            SetTextBox(ContextCheckpointCountBox, value);
            SetComboValue(ContextCheckpointsCombo, "on");
            return;
        }

        if (string.Equals(flagName, "--checkpoint-min-step", StringComparison.OrdinalIgnoreCase))
        {
            SetTextBox(ContextCheckpointEveryNTokensBox, value);
            SetComboValue(ContextCheckpointsCombo, "on");
            return;
        }

        // The vision boolean flags map to the auto/on/off VisionCombo, so translate their
        // parsed true/false into the matching combo item instead of falling back to auto.
        if (string.Equals(flagName, "--no-mmproj", StringComparison.OrdinalIgnoreCase))
        {
            SetComboValue(VisionCombo, IsTruthy(value) ? "off" : "auto");
            return;
        }

        if (string.Equals(flagName, "--mmproj-auto", StringComparison.OrdinalIgnoreCase))
        {
            SetComboValue(VisionCombo, IsTruthy(value) ? "auto" : "off");
            return;
        }

        var firstClass = GetFirstClassControlByFlagName(flagName);
        if (firstClass is not null)
        {
            if (firstClass is WpfTextBox textBox) textBox.Text = value;
            else if (firstClass is WpfComboBox combo) SetComboValue(combo, value);
            return;
        }

        var primary = LocalLlmConsole.Services.LlamaServerFlagSchema.FindByName(flagName)?.PrimaryName ?? flagName;
        if (GeneratedControls.TryGetValue(primary, out var generatedControl))
            LaunchSettingsControlFactory.SetControlValue(generatedControl, value);
    }

    private static bool IsTruthy(string value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);

    private static void SetTextBox(WpfTextBox? textBox, string value)
    {
        if (textBox is not null) textBox.Text = value;
    }

    private static void SetComboValue(WpfComboBox? combo, string value)
    {
        if (combo is null) return;
        // ParseCommand normalizes boolean flags to "true"/"false", but the on/off/auto combos
        // only contain "on"/"off"/"auto", so translate before matching or the value would fall
        // back to the first item and silently discard the user's choice on a save round-trip.
        var normalized = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ? "on"
            : string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ? "off"
            : value;
        var match = combo.Items.Cast<object>().Select(item => item.ToString() ?? "").FirstOrDefault(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase));
        combo.SelectedItem = string.IsNullOrWhiteSpace(match) ? combo.Items[0] : match;
    }

    public void ResetControlsToDefaults()
    {
        // Port, runtime, custom parameters, and the command preview itself are not command flags.
        SetTextBox(ContextSizeBox, SchemaDefault("--ctx-size", "0"));
        SetTextBox(GpuLayersBox, SchemaDefault("--gpu-layers", "0"));
        SetTextBox(ThreadsBox, SchemaDefault("--threads", "0"));
        SetTextBox(ParallelSlotsBox, "1");
        SetTextBox(BatchSizeBox, SchemaDefault("--batch-size", "2048"));
        SetTextBox(MicroBatchSizeBox, SchemaDefault("--ubatch-size", "512"));
        SetTextBox(ReasoningBudgetBox, SchemaDefault("--reasoning-budget", "-1"));
        SetTextBox(VisionImageMinTokensBox, SchemaDefault("--image-min-tokens", "0"));
        SetTextBox(VisionImageMaxTokensBox, SchemaDefault("--image-max-tokens", "0"));
        SetTextBox(TemperatureBox, SchemaDefault("--temp", "0.8"));
        SetTextBox(TopKBox, SchemaDefault("--top-k", "40"));
        SetTextBox(TopPBox, SchemaDefault("--top-p", "0.95"));
        SetTextBox(MinPBox, SchemaDefault("--min-p", "0.05"));
        SetTextBox(MaxTokensBox, SchemaDefault("--predict", "-1"));
        SetTextBox(SeedBox, SchemaDefault("--seed", "-1"));
        SetTextBox(RepeatLastNBox, SchemaDefault("--repeat-last-n", "64"));
        SetTextBox(RepeatPenaltyBox, SchemaDefault("--repeat-penalty", "1"));
        SetTextBox(PresencePenaltyBox, SchemaDefault("--presence-penalty", "0"));
        SetTextBox(FrequencyPenaltyBox, SchemaDefault("--frequency-penalty", "0"));
        SetTextBox(RopeScaleBox, SchemaDefault("--rope-scale", "0"));
        SetTextBox(RopeFreqBaseBox, SchemaDefault("--rope-freq-base", "0"));
        SetTextBox(RopeFreqScaleBox, SchemaDefault("--rope-freq-scale", "0"));
        SetTextBox(SpecDraftModelPathBox, "");
        SetTextBox(MtpHeadPathBox, "");
        SetTextBox(SpecDraftGpuLayersBox, SchemaDefault("--spec-draft-ngl", "0"));
        SetTextBox(SpecDraftMinTokensBox, SchemaDefault("--spec-draft-n-min", "0"));
        SetTextBox(SpecDraftMaxTokensBox, SchemaDefault("--spec-draft-n-max", "3"));
        SetTextBox(SpecDraftPSplitBox, SchemaDefault("--spec-draft-p-split", "0.1"));
        SetTextBox(SpecDraftPMinBox, SchemaDefault("--spec-draft-p-min", "0"));
        SetTextBox(PromptCacheRamMbBox, SchemaDefault("--cache-ram", "8192"));
        SetTextBox(ContextCheckpointCountBox, SchemaDefault("--ctx-checkpoints", "0"));
        SetTextBox(ContextCheckpointEveryNTokensBox, SchemaDefault("--checkpoint-min-step", "8192"));
        SetTextBox(VisionProjectorPathBox, "");

        SetComboValue(MetricsCombo, "off");
        SetComboValue(ReasoningCombo, "auto");
        SetComboValue(ReasoningFormatCombo, "auto");
        SetComboValue(VisionCombo, "auto");
        SetComboValue(FlashAttentionCombo, "auto");
        SetComboValue(CacheTypeKCombo, SchemaDefault("--cache-type-k", "f16"));
        SetComboValue(CacheTypeVCombo, SchemaDefault("--cache-type-v", "f16"));
        SetComboValue(KvOffloadCombo, "auto");
        SetComboValue(KvUnifiedCombo, "auto");
        SetComboValue(PromptCacheCombo, "auto");
        SetComboValue(ContextCheckpointsCombo, "auto");
        SetComboValue(ContinuousBatchingCombo, "on");
        SetComboValue(JinjaCombo, "auto");
        SetComboValue(MmapCombo, "auto");
        SetComboValue(MlockCombo, "off");
        SetComboValue(RopeScalingCombo, "auto");
        SetComboValue(SpeculativeTypeCombo, "none");
        SetComboValue(SpecDraftCacheTypeKCombo, SchemaDefault("--cache-type-k-draft", "f16"));
        SetComboValue(SpecDraftCacheTypeVCombo, SchemaDefault("--cache-type-v-draft", "f16"));

        foreach (var (flagName, control) in GeneratedControls)
        {
            var flag = LlamaServerFlagSchema.FindByName(flagName);
            var value = flag is not null ? NormalizeDefaultValue(flag.Default) : "";
            LaunchSettingsControlFactory.SetControlValue(control, value);
        }
    }

    private static string SchemaDefault(string flagName, string fallback)
    {
        var flag = LlamaServerFlagSchema.FindByName(flagName);
        if (flag?.Default is null) return fallback;
        return Convert.ToString(flag.Default, CultureInfo.InvariantCulture) ?? fallback;
    }

    internal static string NormalizeDefaultValue(object? defaultValue)
    {
        if (defaultValue is null) return "";
        if (defaultValue is bool b) return b ? "on" : "off";
        var s = Convert.ToString(defaultValue, CultureInfo.InvariantCulture) ?? "";
        if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(s, "on", StringComparison.OrdinalIgnoreCase)) return "on";
        if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase)
            || string.Equals(s, "off", StringComparison.OrdinalIgnoreCase)) return "off";
        return s;
    }
}

/// <summary>Reads and applies launch settings values to and from the WPF controls and builds the command preview.</summary>
public static class LaunchSettingsFormBinder
{
    public static AppSettings Read(AppSettings baseSettings, LaunchSettingsFormControls controls, Action<string>? setStatus = null, bool parseCommandPreview = true, IReadOnlySet<string>? supportedFlags = null)
    {
        var next = ReadControls(baseSettings, controls);

        if (controls.CommandPreviewBox is not null && parseCommandPreview)
        {
            var (merged, messages) = ParseAndMergeCommandPreview(next, controls, supportedFlags);
            if (!string.IsNullOrWhiteSpace(messages))
                setStatus?.Invoke(messages);
            next = merged;
        }

        next = next with { FlagValues = ReadGeneratedFlagValues(controls, next.FlagValues) };

        var validationCommand = BuildCommandPreview(next, GetSelectedBackend(controls));
        var validation = LocalLlmConsole.Services.LaunchCommandService.ParseCommand(validationCommand);
        if (validation.Errors.Count > 0)
        {
            var message = string.Join(" ", validation.Errors);
            setStatus?.Invoke(message);
            throw new InvalidOperationException(message);
        }

        ValidateCrossFieldRules(next);
        return next;
    }

    public static void ValidateCommandPreview(LaunchSettingsFormControls controls, Action<string>? setStatus = null, IReadOnlySet<string>? supportedFlags = null)
    {
        if (controls.CommandPreviewBox is null) return;

        var parsed = LocalLlmConsole.Services.LaunchCommandService.ParseCommand(controls.CommandPreviewBox.Text, supportedFlags);
        var messages = new List<string>();
        messages.AddRange(parsed.Errors);
        messages.AddRange(parsed.SecurityWarnings);

        if (parsed.ExtraArgs.Count > 0)
        {
            messages.Add($"Unsupported flags in command preview: {string.Join(", ", parsed.ExtraArgs.Where(t => t.StartsWith("--", StringComparison.Ordinal)))}.");
        }

        var box = controls.CommandPreviewBox;
        if (messages.Count > 0)
        {
            var message = string.Join(" ", messages);
            setStatus?.Invoke(message);
            box.BorderBrush = System.Windows.Media.Brushes.Red;
            box.BorderThickness = new Thickness(1);
            box.ToolTip = message;
        }
        else
        {
            box.BorderBrush = null;
            box.BorderThickness = new Thickness(0);
            box.ToolTip = null;
        }
    }

    private static (AppSettings settings, string messages) ParseAndMergeCommandPreview(AppSettings baseSettings, LaunchSettingsFormControls controls, IReadOnlySet<string>? supportedFlags)
    {
        var previewText = controls.CommandPreviewBox!.Text;
        var parsed = LocalLlmConsole.Services.LaunchCommandService.ParseCommand(previewText, supportedFlags);
        var messages = new List<string>();
        messages.AddRange(parsed.Errors);
        messages.AddRange(parsed.SecurityWarnings);

        controls.ResetControlsToDefaults();

        foreach (var (flagName, value) in parsed.Flags)
            controls.SetValueByFlagName(flagName, value);

        if (parsed.ExtraArgs.Count > 0)
        {
            var existingCustom = LocalLlmConsole.Services.CustomLaunchParameterParser.Parse(controls.CustomParametersBox?.Text ?? "");
            var combined = existingCustom.Concat(parsed.ExtraArgs).Select(QuoteIfNeeded);
            if (controls.CustomParametersBox is not null)
                controls.CustomParametersBox.Text = string.Join(" ", combined);

            messages.Add($"Unsupported flags moved to CustomParameters: {string.Join(", ", parsed.ExtraArgs.Where(t => t.StartsWith("--", StringComparison.Ordinal)))}.");
        }

        var settings = ReadControls(baseSettings, controls);
        return (settings, string.Join(" ", messages));
    }

    private static string QuoteIfNeeded(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (value.Any(c => char.IsWhiteSpace(c) || c == '"' || c == '\\'))
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
        return value;
    }

    public static void Apply(LaunchSettingsFormControls controls, AppSettings settings, Action<string>? setStatus = null)
    {
        SetText(controls.LaunchPortBox, settings.Port);
        SetText(controls.ContextSizeBox, settings.ContextSize);
        SetText(controls.GpuLayersBox, settings.GpuLayers);
        SetText(controls.ParallelSlotsBox, settings.ParallelSlots);
        SetText(controls.BatchSizeBox, settings.BatchSize);
        SetText(controls.MicroBatchSizeBox, settings.MicroBatchSize);
        SetText(controls.ThreadsBox, settings.Threads);
        SetText(controls.ReasoningBudgetBox, settings.ReasoningBudget);
        SetText(controls.VisionProjectorPathBox, settings.VisionProjectorPath);
        SetText(controls.VisionImageMinTokensBox, settings.VisionImageMinTokens);
        SetText(controls.VisionImageMaxTokensBox, settings.VisionImageMaxTokens);
        SetText(controls.TemperatureBox, settings.Temperature);
        SetText(controls.TopKBox, settings.TopK);
        SetText(controls.TopPBox, settings.TopP);
        SetText(controls.MinPBox, settings.MinP);
        SetText(controls.MaxTokensBox, settings.MaxTokens);
        SetText(controls.SeedBox, settings.Seed);
        SetText(controls.RepeatLastNBox, settings.RepeatLastN);
        SetText(controls.RepeatPenaltyBox, settings.RepeatPenalty);
        SetText(controls.PresencePenaltyBox, settings.PresencePenalty);
        SetText(controls.FrequencyPenaltyBox, settings.FrequencyPenalty);
        SetText(controls.RopeScaleBox, settings.RopeScale);
        SetText(controls.RopeFreqBaseBox, settings.RopeFreqBase);
        SetText(controls.RopeFreqScaleBox, settings.RopeFreqScale);
        SetText(controls.SpecDraftModelPathBox, settings.SpecDraftModelPath);
        SetText(controls.MtpHeadPathBox, settings.MtpHeadPath);
        SetText(controls.SpecDraftGpuLayersBox, settings.SpecDraftGpuLayers);
        SetText(controls.SpecDraftMinTokensBox, settings.SpecDraftMinTokens);
        SetText(controls.SpecDraftMaxTokensBox, settings.SpecDraftMaxTokens);
        SetText(controls.SpecDraftPSplitBox, settings.SpecDraftPSplit);
        SetText(controls.SpecDraftPMinBox, settings.SpecDraftPMin);
        SetText(controls.CustomParametersBox, settings.CustomParameters);
        SetCombo(controls.MetricsCombo, settings.EnableMetrics ? "on" : "off");
        SetCombo(controls.ReasoningCombo, settings.ReasoningMode);
        SetCombo(controls.ReasoningFormatCombo, settings.ReasoningFormat);
        SetCombo(controls.VisionCombo, settings.VisionMode);
        SetCombo(controls.FlashAttentionCombo, settings.FlashAttention);
        SetCombo(controls.CacheTypeKCombo, settings.CacheTypeK);
        SetCombo(controls.CacheTypeVCombo, settings.CacheTypeV);
        SetCombo(controls.KvOffloadCombo, settings.KvOffload);
        SetCombo(controls.KvUnifiedCombo, settings.KvUnified);
        SetCombo(controls.PromptCacheCombo, settings.PromptCacheMode);
        SetText(controls.PromptCacheRamMbBox, settings.PromptCacheRamMb);
        SetCombo(controls.ContextCheckpointsCombo, settings.ContextCheckpointsMode);
        SetText(controls.ContextCheckpointCountBox, settings.ContextCheckpointCount);
        SetText(controls.ContextCheckpointEveryNTokensBox, settings.ContextCheckpointEveryNTokens);
        SetCombo(controls.ContinuousBatchingCombo, settings.ContinuousBatching);
        SetCombo(controls.JinjaCombo, settings.JinjaMode);
        SetCombo(controls.MmapCombo, settings.MmapMode);
        SetCombo(controls.MlockCombo, settings.MlockMode);
        SetCombo(controls.RopeScalingCombo, settings.RopeScaling);
        SetCombo(controls.SpeculativeTypeCombo, LocalLlmConsole.Services.LaunchSettingMetadataService.NormalizeSpeculativeType(settings.SpeculativeType));
        SetCombo(controls.SpecDraftCacheTypeKCombo, settings.SpecDraftCacheTypeK);
        SetCombo(controls.SpecDraftCacheTypeVCombo, settings.SpecDraftCacheTypeV);

        ApplyGeneratedFlagValues(controls, settings.FlagValues);

        if (controls.CommandPreviewBox is not null)
        {
            try
            {
                controls.CommandPreviewBox.Text = BuildCommandPreview(settings, GetSelectedBackend(controls));
            }
            catch (Exception ex)
            {
                setStatus?.Invoke($"Command preview failed: {ex.Message}");
            }
        }
    }

    public static void AttachChangeHandlers(LaunchSettingsFormControls controls, Action changed, RoutedEventHandler contextSizeLostFocus, Action? commandPreviewChanged = null, Action? updatePreview = null, Action? validateCommandPreview = null)
    {
        if (controls.ContextSizeBox is not null)
            controls.ContextSizeBox.LostFocus += contextSizeLostFocus;

        foreach (var box in controls.TextBoxes.Where(box => box is not null))
        {
            box!.TextChanged += (_, _) => changed();
            if (updatePreview is not null)
                box.LostFocus += (_, _) => updatePreview();
        }

        foreach (var combo in controls.ComboBoxes.Where(combo => combo is not null))
        {
            combo!.SelectionChanged += (_, _) => changed();
            if (updatePreview is not null)
                combo.SelectionChanged += (_, _) => updatePreview();
        }

        foreach (var generated in controls.GeneratedControls.Values)
        {
            if (LaunchSettingsControlFactory.FindEditor(generated) is WpfTextBox textBox)
            {
                textBox.TextChanged += (_, _) => changed();
                if (updatePreview is not null)
                    textBox.LostFocus += (_, _) => updatePreview();
            }
            else if (generated is WpfComboBox combo)
            {
                combo.SelectionChanged += (_, _) => changed();
                if (updatePreview is not null)
                    combo.SelectionChanged += (_, _) => updatePreview();
            }
        }

        if (controls.CommandPreviewBox is not null)
        {
            if (commandPreviewChanged is not null)
                controls.CommandPreviewBox.TextChanged += (_, _) => commandPreviewChanged();
            if (validateCommandPreview is not null)
            {
                controls.CommandPreviewBox.LostFocus += (_, _) => validateCommandPreview();
                controls.CommandPreviewBox.KeyDown += (_, e) =>
                {
                    if (e.Key == Key.Enter)
                        validateCommandPreview();
                };
            }
        }
    }

    public static string BuildCommandPreview(AppSettings settings, RuntimeBackend backend = RuntimeBackend.Cpu)
    {
        var options = BuildLaunchOptions(settings, backend);
        return LocalLlmConsole.Services.LaunchCommandService.BuildCommand(options);
    }

    private static RuntimeBackend GetSelectedBackend(LaunchSettingsFormControls controls)
    {
        if (controls.RuntimeCombo?.SelectedItem is RuntimeChoice choice)
            return choice.Backend;
        return RuntimeBackend.Cpu;
    }

    public static void ValidateCrossFieldRules(AppSettings next)
    {
        if (next.SpecDraftPSplit < 0 && Math.Abs(next.SpecDraftPSplit + 1) > 0.000_001)
            throw new InvalidOperationException("Draft split probability must be -1 for default or between 0 and 1.");
        if (next.SpecDraftPMin < 0 && Math.Abs(next.SpecDraftPMin + 1) > 0.000_001)
            throw new InvalidOperationException("Draft min probability must be -1 for default or between 0 and 1.");
        if (string.Equals(next.PromptCacheMode, "on", StringComparison.OrdinalIgnoreCase) && next.PromptCacheRamMb == 0)
            throw new InvalidOperationException("Prompt cache MB must be -1 or greater than 0 when prompt cache is on.");
        if (string.Equals(next.ContextCheckpointsMode, "on", StringComparison.OrdinalIgnoreCase) && next.ContextCheckpointCount < 1)
            throw new InvalidOperationException("Checkpoint count must be at least 1 when checkpoints are on.");
        if (string.Equals(next.ContextCheckpointsMode, "on", StringComparison.OrdinalIgnoreCase) && next.ContextCheckpointEveryNTokens < 1)
            throw new InvalidOperationException("Checkpoint spacing must be at least 1 when checkpoints are on.");
        if (next.SpecDraftMaxTokens > 0 && next.SpecDraftMinTokens > next.SpecDraftMaxTokens)
            throw new InvalidOperationException("Draft min tokens cannot be larger than draft max tokens.");
        if (next.VisionImageMaxTokens > 0 && next.VisionImageMinTokens > next.VisionImageMaxTokens)
            throw new InvalidOperationException("Image min tokens cannot be larger than image max tokens.");
        _ = LocalLlmConsole.Services.CustomLaunchParameterParser.Parse(next.CustomParameters);
    }

    private static void SetText(WpfTextBox? box, int value) => SetText(box, value.ToString(CultureInfo.InvariantCulture));

    private static void SetText(WpfTextBox? box, double value) => SetText(box, value.ToString("0.###", CultureInfo.InvariantCulture));

    private static void SetText(WpfTextBox? box, string value)
    {
        if (box is not null) box.Text = value;
    }

    private static void SetCombo(WpfComboBox? combo, string value)
    {
        if (combo is null) return;
        var match = combo.Items.Cast<object>().Select(item => item.ToString() ?? "").FirstOrDefault(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
        combo.SelectedItem = string.IsNullOrWhiteSpace(match) ? combo.Items[0] : match;
    }

    private static AppSettings ReadControls(AppSettings baseSettings, LaunchSettingsFormControls controls)
        => baseSettings with
        {
            Port = ReadInt(controls.LaunchPortBox, "Port", min: 1, max: 65535),
            ContextSize = ReadContextSize(controls.ContextSizeBox),
            GpuLayers = ReadInt(controls.GpuLayersBox, "GPU layers", min: 0),
            ParallelSlots = ReadInt(controls.ParallelSlotsBox, "Parallel slots", min: 1),
            BatchSize = ReadInt(controls.BatchSizeBox, "Batch size", min: 1),
            MicroBatchSize = ReadInt(controls.MicroBatchSizeBox, "Micro batch size", min: 1),
            Threads = ReadInt(controls.ThreadsBox, "Threads", min: 0),
            ReasoningMode = ComboValue(controls.ReasoningCombo),
            ReasoningFormat = ComboValue(controls.ReasoningFormatCombo),
            ReasoningBudget = ReadInt(controls.ReasoningBudgetBox, "Reasoning budget", min: -1),
            VisionMode = ComboValue(controls.VisionCombo),
            VisionProjectorPath = controls.VisionProjectorPathBox?.Text.Trim() ?? "",
            VisionImageMinTokens = ReadInt(controls.VisionImageMinTokensBox, "Image min tokens", min: 0),
            VisionImageMaxTokens = ReadInt(controls.VisionImageMaxTokensBox, "Image max tokens", min: 0),
            FlashAttention = ComboValue(controls.FlashAttentionCombo),
            CacheTypeK = ComboValue(controls.CacheTypeKCombo),
            CacheTypeV = ComboValue(controls.CacheTypeVCombo),
            KvOffload = ComboValue(controls.KvOffloadCombo),
            KvUnified = ComboValue(controls.KvUnifiedCombo),
            PromptCacheMode = ComboValue(controls.PromptCacheCombo),
            PromptCacheRamMb = ReadInt(controls.PromptCacheRamMbBox, "Prompt cache MB", min: -1),
            ContextCheckpointsMode = ComboValue(controls.ContextCheckpointsCombo),
            ContextCheckpointCount = ReadInt(controls.ContextCheckpointCountBox, "Checkpoint count", min: 0),
            ContextCheckpointEveryNTokens = ReadInt(controls.ContextCheckpointEveryNTokensBox, "Checkpoint spacing", min: -1),
            ContinuousBatching = ComboValue(controls.ContinuousBatchingCombo),
            JinjaMode = ComboValue(controls.JinjaCombo),
            MmapMode = ComboValue(controls.MmapCombo),
            MlockMode = ComboValue(controls.MlockCombo),
            EnableMetrics = ComboValue(controls.MetricsCombo) == "on",
            Temperature = ReadDouble(controls.TemperatureBox, "Temperature", min: 0),
            TopK = ReadInt(controls.TopKBox, "Top K", min: 0),
            TopP = ReadDouble(controls.TopPBox, "Top P", min: 0, max: 1),
            MinP = ReadDouble(controls.MinPBox, "Min P", min: 0, max: 1),
            MaxTokens = ReadInt(controls.MaxTokensBox, "Max tokens", min: -1),
            Seed = ReadInt(controls.SeedBox, "Seed", min: -1),
            RepeatLastN = ReadInt(controls.RepeatLastNBox, "Repeat window", min: -1),
            RepeatPenalty = ReadDouble(controls.RepeatPenaltyBox, "Repeat penalty", min: 0),
            PresencePenalty = ReadDouble(controls.PresencePenaltyBox, "Presence penalty", min: -10, max: 10),
            FrequencyPenalty = ReadDouble(controls.FrequencyPenaltyBox, "Frequency penalty", min: -10, max: 10),
            RopeScaling = ComboValue(controls.RopeScalingCombo),
            RopeScale = ReadDouble(controls.RopeScaleBox, "RoPE scale", min: 0),
            RopeFreqBase = ReadDouble(controls.RopeFreqBaseBox, "RoPE base", min: 0),
            RopeFreqScale = ReadDouble(controls.RopeFreqScaleBox, "RoPE frequency scale", min: 0),
            SpeculativeType = ComboValue(controls.SpeculativeTypeCombo),
            SpecDraftModelPath = controls.SpecDraftModelPathBox?.Text.Trim() ?? "",
            MtpHeadPath = controls.MtpHeadPathBox?.Text.Trim() ?? "",
            SpecDraftGpuLayers = ReadInt(controls.SpecDraftGpuLayersBox, "Draft GPU layers", min: -1),
            SpecDraftMinTokens = ReadInt(controls.SpecDraftMinTokensBox, "Draft min tokens", min: 0),
            SpecDraftMaxTokens = ReadInt(controls.SpecDraftMaxTokensBox, "Draft max tokens", min: 0),
            SpecDraftPSplit = ReadDouble(controls.SpecDraftPSplitBox, "Draft split probability", min: -1, max: 1),
            SpecDraftPMin = ReadDouble(controls.SpecDraftPMinBox, "Draft min probability", min: -1, max: 1),
            SpecDraftCacheTypeK = ComboValue(controls.SpecDraftCacheTypeKCombo),
            SpecDraftCacheTypeV = ComboValue(controls.SpecDraftCacheTypeVCombo),
            CustomParameters = controls.CustomParametersBox?.Text.Trim() ?? ""
        };

    private static IReadOnlyDictionary<string, string> ReadGeneratedFlagValues(LaunchSettingsFormControls controls, IReadOnlyDictionary<string, string> existing)
    {
        var merged = new Dictionary<string, string>(existing, StringComparer.OrdinalIgnoreCase);
        foreach (var (flagName, control) in controls.GeneratedControls)
        {
            var value = LaunchSettingsControlFactory.GetControlValue(control);
            if (string.IsNullOrWhiteSpace(value)) continue;
            var primary = LlamaServerFlagSchema.FindByName(flagName)?.PrimaryName ?? flagName;
            merged[primary] = value;
        }
        return merged.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static void ApplyGeneratedFlagValues(LaunchSettingsFormControls controls, IReadOnlyDictionary<string, string> flagValues)
    {
        foreach (var (flagName, control) in controls.GeneratedControls)
        {
            var primary = LlamaServerFlagSchema.FindByName(flagName)?.PrimaryName ?? flagName;
            if (flagValues.TryGetValue(primary, out var value))
                LaunchSettingsControlFactory.SetControlValue(control, value);
        }
    }

    private static LlamaServerLaunchOptions BuildLaunchOptions(AppSettings settings, RuntimeBackend backend)
    {
        return new LlamaServerLaunchOptions
        {
            Backend = backend,
            ModelPath = "",
            Host = "127.0.0.1",
            Port = settings.Port,
            ContextSize = settings.ContextSize,
            GpuLayers = settings.GpuLayers,
            EnableMetrics = settings.EnableMetrics,
            ParallelSlots = settings.ParallelSlots,
            BatchSize = settings.BatchSize,
            MicroBatchSize = settings.MicroBatchSize,
            Threads = settings.Threads,
            FlashAttention = settings.FlashAttention,
            CacheTypeK = settings.CacheTypeK,
            CacheTypeV = settings.CacheTypeV,
            KvOffload = settings.KvOffload,
            KvUnified = settings.KvUnified,
            PromptCacheMode = settings.PromptCacheMode,
            PromptCacheRamMb = settings.PromptCacheRamMb,
            ContextCheckpointsMode = settings.ContextCheckpointsMode,
            ContextCheckpointCount = settings.ContextCheckpointCount,
            ContextCheckpointEveryNTokens = settings.ContextCheckpointEveryNTokens,
            ContinuousBatching = settings.ContinuousBatching,
            ReasoningMode = settings.ReasoningMode,
            ReasoningFormat = settings.ReasoningFormat,
            ReasoningBudget = settings.ReasoningBudget,
            JinjaMode = settings.JinjaMode,
            VisionMode = settings.VisionMode,
            VisionProjectorPath = settings.VisionProjectorPath,
            VisionProjectorEmbedded = false,
            VisionImageMinTokens = settings.VisionImageMinTokens,
            VisionImageMaxTokens = settings.VisionImageMaxTokens,
            MmapMode = settings.MmapMode,
            MlockMode = settings.MlockMode,
            Temperature = settings.Temperature,
            TopK = settings.TopK,
            TopP = settings.TopP,
            MinP = settings.MinP,
            MaxTokens = settings.MaxTokens,
            Seed = settings.Seed,
            RepeatLastN = settings.RepeatLastN,
            RepeatPenalty = settings.RepeatPenalty,
            PresencePenalty = settings.PresencePenalty,
            FrequencyPenalty = settings.FrequencyPenalty,
            RopeScaling = settings.RopeScaling,
            RopeScale = settings.RopeScale,
            RopeFreqBase = settings.RopeFreqBase,
            RopeFreqScale = settings.RopeFreqScale,
            SpeculativeType = settings.SpeculativeType,
            SpecDraftModelPath = settings.SpecDraftModelPath,
            MtpHeadPath = settings.MtpHeadPath,
            SpecDraftGpuLayers = settings.SpecDraftGpuLayers,
            SpecDraftMinTokens = settings.SpecDraftMinTokens,
            SpecDraftMaxTokens = settings.SpecDraftMaxTokens,
            SpecDraftPSplit = settings.SpecDraftPSplit,
            SpecDraftPMin = settings.SpecDraftPMin,
            SpecDraftCacheTypeK = settings.SpecDraftCacheTypeK,
            SpecDraftCacheTypeV = settings.SpecDraftCacheTypeV,
            FlagValues = settings.FlagValues
        };
    }

    private static string ComboValue(WpfComboBox? combo)
        => (combo?.SelectedItem?.ToString() ?? combo?.Text ?? "").Trim().ToLowerInvariant();

    private static int ReadContextSize(WpfTextBox? box)
        => LaunchSettingParser.ReadContextSize(box?.Text.Trim() ?? "");

    private static int ReadInt(WpfTextBox? box, string label, int min, int? max = null)
        => LaunchSettingParser.ReadInt(box?.Text.Trim() ?? "", label, min, max);

    private static double ReadDouble(WpfTextBox? box, string label, double min, double? max = null)
        => LaunchSettingParser.ReadDouble(box?.Text.Trim() ?? "", label, min, max);
}
