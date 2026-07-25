using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfControl = System.Windows.Controls.Control;
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
    public TextBlock? RuntimeDiscoveredFlagsText { get; set; }
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

    public List<string> FlagOrder { get; set; } = [];

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

    // Accessors rather than a control map: this is consulted for every flag row on every
    // keystroke, and the properties are assigned progressively while the panel is built, so a
    // captured control reference could be stale. Built once per process; lookups allocate nothing.
    private static readonly Dictionary<string, Func<LaunchSettingsFormControls, FrameworkElement?>> FirstClassControlAccessors =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["--ctx-size"] = c => c.ContextSizeBox,
            ["--threads"] = c => c.ThreadsBox,
            ["--gpu-layers"] = c => c.GpuLayersBox,
            ["--n-gpu-layers"] = c => c.GpuLayersBox,
            ["--batch-size"] = c => c.BatchSizeBox,
            ["--ubatch-size"] = c => c.MicroBatchSizeBox,
            ["--flash-attn"] = c => c.FlashAttentionCombo,
            ["--cache-type-k"] = c => c.CacheTypeKCombo,
            ["--cache-type-v"] = c => c.CacheTypeVCombo,
            ["--kv-offload"] = c => c.KvOffloadCombo,
            ["--kv-unified"] = c => c.KvUnifiedCombo,
            ["--cache-ram-mode"] = c => c.PromptCacheCombo,
            ["--cache-ram"] = c => c.PromptCacheRamMbBox,
            ["--ctx-checkpoints-mode"] = c => c.ContextCheckpointsCombo,
            ["--ctx-checkpoints"] = c => c.ContextCheckpointCountBox,
            ["--checkpoint-min-step"] = c => c.ContextCheckpointEveryNTokensBox,
            ["--cont-batching"] = c => c.ContinuousBatchingCombo,
            ["--mmap"] = c => c.MmapCombo,
            ["--mlock"] = c => c.MlockCombo,
            ["--reasoning"] = c => c.ReasoningCombo,
            ["--reasoning-format"] = c => c.ReasoningFormatCombo,
            ["--reasoning-budget"] = c => c.ReasoningBudgetBox,
            ["--jinja"] = c => c.JinjaCombo,
            ["--mmproj-auto"] = c => c.VisionCombo,
            ["--no-mmproj"] = c => c.VisionCombo,
            ["--mmproj"] = c => c.VisionProjectorPathBox,
            ["--image-min-tokens"] = c => c.VisionImageMinTokensBox,
            ["--image-max-tokens"] = c => c.VisionImageMaxTokensBox,
            ["--temp"] = c => c.TemperatureBox,
            ["--top-k"] = c => c.TopKBox,
            ["--top-p"] = c => c.TopPBox,
            ["--min-p"] = c => c.MinPBox,
            ["--predict"] = c => c.MaxTokensBox,
            ["--n-predict"] = c => c.MaxTokensBox,
            ["--seed"] = c => c.SeedBox,
            ["--repeat-last-n"] = c => c.RepeatLastNBox,
            ["--repeat-penalty"] = c => c.RepeatPenaltyBox,
            ["--presence-penalty"] = c => c.PresencePenaltyBox,
            ["--frequency-penalty"] = c => c.FrequencyPenaltyBox,
            ["--rope-scaling"] = c => c.RopeScalingCombo,
            ["--rope-scale"] = c => c.RopeScaleBox,
            ["--rope-freq-base"] = c => c.RopeFreqBaseBox,
            ["--rope-freq-scale"] = c => c.RopeFreqScaleBox,
            ["--spec-type"] = c => c.SpeculativeTypeCombo,
            ["--model-draft"] = c => c.SpecDraftModelPathBox,
            ["--spec-draft-model"] = c => c.SpecDraftModelPathBox,
            ["--mtp-head"] = c => c.MtpHeadPathBox,
            ["--cache-type-k-draft"] = c => c.SpecDraftCacheTypeKCombo,
            ["--cache-type-v-draft"] = c => c.SpecDraftCacheTypeVCombo,
            ["--spec-draft-n-max"] = c => c.SpecDraftMaxTokensBox,
            ["--spec-draft-n-min"] = c => c.SpecDraftMinTokensBox,
            ["--spec-draft-ngl"] = c => c.SpecDraftGpuLayersBox,
            ["--gpu-layers-draft"] = c => c.SpecDraftGpuLayersBox,
            ["--n-gpu-layers-draft"] = c => c.SpecDraftGpuLayersBox,
            ["--spec-draft-p-split"] = c => c.SpecDraftPSplitBox,
            ["--spec-draft-p-min"] = c => c.SpecDraftPMinBox,
            ["--parallel"] = c => c.ParallelSlotsBox,
            ["--metrics"] = c => c.MetricsCombo,
            ["--custom-params"] = c => c.CustomParametersBox
        };

    public FrameworkElement? GetFirstClassControlByFlagName(string flagName)
    {
        if (FirstClassControlAccessors.TryGetValue(flagName, out var accessor)) return accessor(this);

        // ParseCommand keys flags by the exact alias typed (e.g. "-ngl", "-c"), so a pasted
        // short-form flag would miss the long-name map. Fall back to the flag's other names
        // to keep first-class fields in sync instead of silently dropping the value.
        var schemaFlag = LocalLlmConsole.Services.LlamaServerFlagSchema.FindByName(flagName);
        if (schemaFlag is not null)
        {
            foreach (var name in schemaFlag.Names)
                if (FirstClassControlAccessors.TryGetValue(name, out var aliasAccessor)) return aliasAccessor(this);
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
            // The builder emits "--ctx-checkpoints 0" for the off mode (mirroring --cache-ram
            // above), so zero must map back to off; forcing "on" produces an on-with-zero-count
            // combo the cross-field validator rejects.
            SetComboValue(ContextCheckpointsCombo, string.Equals(value.Trim(), "0", StringComparison.Ordinal) ? "off" : "on");
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
        if (string.Equals(flagName, "--no-mmproj", StringComparison.OrdinalIgnoreCase)
            || string.Equals(flagName, "--no-mmproj-auto", StringComparison.OrdinalIgnoreCase))
        {
            // The parser reports the negation's state as true/false; only an enabled negation
            // ("--no-mmproj" or "--no-mmproj true") disables vision. "--no-mmproj false" means
            // the projector stays enabled, so the combo must keep its current selection.
            if (!string.Equals(value.Trim(), "false", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value.Trim(), "off", StringComparison.OrdinalIgnoreCase))
                SetComboValue(VisionCombo, "off");
            return;
        }

        if (string.Equals(flagName, "--mmproj-auto", StringComparison.OrdinalIgnoreCase))
        {
            SetComboValue(VisionCombo, IsEnabledOrAuto(value) ? "auto" : "off");
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

    private static void SetTextBox(WpfTextBox? textBox, string value)
    {
        if (textBox is not null) textBox.Text = value;
    }

    // Command-line round-tripping: an unmatched value means the user typed something this
    // combo cannot represent, so leave it unselected rather than silently picking an item.
    private static void SetComboValue(WpfComboBox? combo, string value)
    {
        if (combo is null) return;
        LaunchSettingsControlFactory.SetComboValue(combo, value);
    }

    // Unlike the shared boolean vocabulary in LaunchCommandService, "auto" counts as enabled
    // here: the vision flags treat auto-detection as the projector being in use.
    private static bool IsEnabledOrAuto(string value)
        => LocalLlmConsole.Services.LaunchCommandService.IsTruthyBoolean(value)
            || string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase);

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
            LaunchSettingsControlFactory.SetControlValue(control, "");
        }
    }

    private static string SchemaDefault(string flagName, string fallback)
    {
        var flag = LlamaServerFlagSchema.FindByName(flagName);
        if (flag?.Default is null) return fallback;
        return Convert.ToString(flag.Default, CultureInfo.InvariantCulture) ?? fallback;
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

        var flagValidation = LocalLlmConsole.Services.LaunchCommandValidator.Validate(next.FlagValues, validateFilePaths: false);
        if (!flagValidation.Ok)
        {
            var message = string.Join(" ", flagValidation.Errors);
            setStatus?.Invoke(message);
            throw new InvalidOperationException(message);
        }

        var validationCommand = BuildCommandPreview(next, GetSelectedBackend(controls), controls.FlagOrder);
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

    public static void ApplyCommandPreviewVisualState(
        LaunchSettingsFormControls controls,
        IReadOnlyList<string> errors,
        Action<string>? setStatus = null,
        bool clearStatusOnSuccess = true)
    {
        var box = controls.CommandPreviewBox;
        if (box is null) return;

        if (errors.Count > 0)
        {
            var message = string.Join(" ", errors);
            setStatus?.Invoke(message);
            LaunchInputVisualState.SetState(box, LaunchInputState.Invalid);
            box.BorderBrush = System.Windows.Media.Brushes.Red;
            box.BorderThickness = new Thickness(1);
            box.ToolTip = message;
        }
        else
        {
            if (clearStatusOnSuccess)
                setStatus?.Invoke("");
            var accent = (System.Windows.Media.Brush?)System.Windows.Application.Current?.Resources["Accent"] ?? System.Windows.Media.Brushes.Green;
            LaunchInputVisualState.SetState(box, LaunchInputState.Valid);
            box.BorderBrush = accent;
            box.BorderThickness = new Thickness(1);
            box.ToolTip = Loc.T("Tooltip.Field.CommandLine");
        }
    }

    public static void ApplyFormDrivenCommandPreviewState(
        LaunchSettingsFormControls controls,
        IReadOnlyList<string> formErrors,
        Action<string>? setStatus = null)
    {
        // The command preview contains only values that could be normalized and emitted.
        // Its border therefore reflects its own parse result, while form errors remain in
        // the status area and on the individual form controls.
        ValidateCommandPreview(controls, setStatus: null, supportedFlags: null, isUserEdit: false);
        setStatus?.Invoke(formErrors.Count > 0 ? string.Join(" ", formErrors) : "");
    }

    public static void ValidateCommandPreview(LaunchSettingsFormControls controls, Action<string>? setStatus = null, IReadOnlySet<string>? supportedFlags = null, bool isUserEdit = true)
    {
        if (controls.CommandPreviewBox is null) return;

        var parsed = LocalLlmConsole.Services.LaunchCommandService.ParseCommand(controls.CommandPreviewBox.Text, supportedFlags);
        var messages = new List<string>();
        messages.AddRange(parsed.Errors);
        messages.AddRange(parsed.SecurityWarnings);

        var unknownFlagTokens = parsed.ExtraArgs.Where(t => t.StartsWith("-", StringComparison.Ordinal)).ToList();
        if (unknownFlagTokens.Count > 0)
        {
            messages.Add($"Unknown flags: {string.Join(", ", unknownFlagTokens)}.");
        }

        ApplyCommandPreviewVisualState(controls, messages, setStatus, clearStatusOnSuccess: isUserEdit);
    }

    public static void UpdateFlagVisualStates(LaunchSettingsFormControls controls, LaunchSettingsPanelState? state)
    {
        if (state is null) return;

        var accent = System.Windows.Application.Current?.Resources["Accent"] as System.Windows.Media.Brush
            ?? System.Windows.Media.Brushes.Green;
        var muted = System.Windows.Application.Current?.Resources["TextMuted"] as System.Windows.Media.Brush
            ?? System.Windows.Media.Brushes.Gray;

        foreach (var elements in state.LaunchSettingElements.Values)
        {
            if (elements.Count < 2 || elements[0] is not TextBlock label) continue;
            var control = elements[1];
            var flagName = control.Tag as string;
            if (string.IsNullOrWhiteSpace(flagName) || !flagName.StartsWith("-", StringComparison.Ordinal)) continue;

            controls.TryGetValueByFlagName(flagName, out var value);
            var flag = LlamaServerFlagSchema.FindByName(flagName);
            var isDefault = IsDefaultFlagFormValue(flagName, flag, value);
            var isValid = flag is null
                ? IsValidFlagFormValue(flagName, flag, value)
                : isDefault || IsValidFlagFormValue(flagName, flag, value);
            var color = !isValid
                ? System.Windows.Media.Brushes.Red
                : isDefault ? null : accent;

            label.Foreground = color ?? muted;
            var visualState = !isValid
                ? LaunchInputState.Invalid
                : isDefault ? LaunchInputState.Default : LaunchInputState.Valid;
            ApplyFlagControlBorder(control, color, visualState);
        }
    }

    private static bool IsDefaultFlagFormValue(string flagName, LlamaServerFlag? flag, string value)
    {
        if (flag is not null)
            return LaunchCommandService.IsDefaultFlagValue(flag, value);

        return flagName.ToLowerInvariant() switch
        {
            "--cache-ram-mode" or "--ctx-checkpoints-mode" =>
                string.IsNullOrWhiteSpace(value) || string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase),
            "--custom-params" => string.IsNullOrWhiteSpace(value),
            _ => true
        };
    }

    private static bool IsValidFlagFormValue(string flagName, LlamaServerFlag? flag, string value)
    {
        if (flag is null)
        {
            if (!string.Equals(flagName, "--custom-params", StringComparison.OrdinalIgnoreCase))
                return true;

            try
            {
                _ = CustomLaunchParameterParser.Parse(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // The form uses -1 as an explicit "unset/use runtime default" sentinel for these
        // first-class settings even though the corresponding llama-server flags start at 0.
        if ((string.Equals(flag.PrimaryName, "--spec-draft-ngl", StringComparison.OrdinalIgnoreCase)
             || string.Equals(flag.PrimaryName, "--spec-draft-p-split", StringComparison.OrdinalIgnoreCase)
             || string.Equals(flag.PrimaryName, "--spec-draft-p-min", StringComparison.OrdinalIgnoreCase))
            && string.Equals(value.Trim(), "-1", StringComparison.Ordinal))
        {
            return true;
        }

        return LaunchCommandValidator.ValidateValue(flag, value, validateFilePaths: false).Ok;
    }

    private static void ApplyFlagControlBorder(FrameworkElement control, System.Windows.Media.Brush? color, LaunchInputState visualState)
    {
        var editor = control as WpfControl;
        if (editor is null && control is Grid grid)
        {
            editor = grid.Children
                .OfType<WpfControl>()
                .FirstOrDefault(candidate => candidate.Visibility == Visibility.Visible)
                ?? LaunchSettingsControlFactory.FindEditor(control) as WpfControl;
        }
        if (editor is null) return;

        LaunchInputVisualState.SetState(editor, visualState);
        if (color is null)
        {
            editor.ClearValue(WpfControl.BorderBrushProperty);
            editor.ClearValue(WpfControl.BorderThicknessProperty);
            return;
        }

        editor.BorderBrush = color;
        editor.BorderThickness = new Thickness(1);
    }

    private static (AppSettings settings, string messages) ParseAndMergeCommandPreview(AppSettings baseSettings, LaunchSettingsFormControls controls, IReadOnlySet<string>? supportedFlags)
    {
        var previewText = controls.CommandPreviewBox!.Text;
        var parsed = LocalLlmConsole.Services.LaunchCommandService.ParseCommand(previewText, supportedFlags);
        var messages = new List<string>();
        messages.AddRange(parsed.Errors);
        messages.AddRange(parsed.SecurityWarnings);

        if (parsed.Errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", messages));

        controls.ResetControlsToDefaults();

        foreach (var (flagName, value) in parsed.Flags)
            controls.SetValueByFlagName(flagName, value);

        if (parsed.ExtraArgs.Count > 0)
        {
            var existingCustom = LocalLlmConsole.Services.CustomLaunchParameterParser.Parse(controls.CustomParametersBox?.Text ?? "");
            var combined = existingCustom.Concat(parsed.ExtraArgs)
                .Select(LocalLlmConsole.Services.LaunchCommandService.QuoteIfNeeded);
            if (controls.CustomParametersBox is not null)
                controls.CustomParametersBox.Text = string.Join(" ", combined);

            messages.Add($"Unsupported flags moved to CustomParameters: {string.Join(", ", parsed.ExtraArgs.Where(t => t.StartsWith("-", StringComparison.Ordinal)))}.");
        }

        var settings = ReadControls(baseSettings, controls);
        return (settings, string.Join(" ", messages));
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
                controls.CommandPreviewBox.Text = BuildCommandPreview(settings, GetSelectedBackend(controls), controls.FlagOrder);
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
                box.TextChanged += (_, _) => updatePreview();
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
                    textBox.TextChanged += (_, _) => updatePreview();
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
            if (validateCommandPreview is not null)
            {
                controls.CommandPreviewBox.TextChanged += (_, _) => validateCommandPreview();
            }

            if (commandPreviewChanged is not null)
            {
                controls.CommandPreviewBox.LostFocus += (_, _) => commandPreviewChanged();
                controls.CommandPreviewBox.KeyDown += (_, e) =>
                {
                    if (e.Key == Key.Enter)
                        commandPreviewChanged();
                };
            }
        }
    }

    public static string BuildCommandPreview(AppSettings settings, RuntimeBackend backend = RuntimeBackend.Cpu, IReadOnlyList<string>? flagOrder = null)
    {
        var options = BuildLaunchOptions(settings, backend, flagOrder);
        return LocalLlmConsole.Services.LaunchCommandService.BuildCommand(options);
    }

    internal static RuntimeBackend GetSelectedBackend(LaunchSettingsFormControls controls)
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

    // Binding saved settings into the form: a stored value the combo no longer offers falls
    // back to the first item so the control always shows something selectable.
    private static void SetCombo(WpfComboBox? combo, string value)
    {
        if (combo is null) return;
        LaunchSettingsControlFactory.SetComboValue(combo, value, fallbackToFirstItem: true);
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
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in LocalLlmConsole.Services.LaunchCommandService.CanonicalizeFlagValues(existing))
        {
            if (IsManagedOrSecurityCriticalFlag(kvp.Key)) continue;
            merged[kvp.Key] = kvp.Value;
        }
        foreach (var (flagName, control) in controls.GeneratedControls)
        {
            if (IsManagedOrSecurityCriticalFlag(flagName)) continue;
            var primary = LlamaServerFlagSchema.FindByName(flagName)?.PrimaryName ?? flagName;
            var value = LaunchSettingsControlFactory.GetControlValue(control);
            var normalized = NormalizeGeneratedControlValue(primary, value);
            if (string.IsNullOrWhiteSpace(normalized))
                merged.Remove(primary);
            else
                merged[primary] = normalized;
        }
        return merged;
    }

    private static string? NormalizeGeneratedControlValue(string primary, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var flag = LlamaServerFlagSchema.FindByName(primary);
        if (flag?.ValueType != FlagValueType.Boolean) return value.Trim();

        var v = value.Trim();
        if (string.Equals(v, "on", StringComparison.OrdinalIgnoreCase)
            || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase))
            return "true";

        if (string.Equals(v, "off", StringComparison.OrdinalIgnoreCase)
            || string.Equals(v, "false", StringComparison.OrdinalIgnoreCase))
            return flag.NegatedForm is not null ? "false" : null;

        if (string.Equals(v, "auto", StringComparison.OrdinalIgnoreCase))
            return null;

        return v;
    }

    private static bool IsManagedOrSecurityCriticalFlag(string flagName)
    {
        if (string.Equals(flagName, "--model", StringComparison.OrdinalIgnoreCase)) return true;
        return LlamaServerFlagSchema.FindByName(flagName)?.IsSecurityCritical == true;
    }

    private static void ApplyGeneratedFlagValues(LaunchSettingsFormControls controls, IReadOnlyDictionary<string, string> flagValues)
    {
        var canonical = LocalLlmConsole.Services.LaunchCommandService.CanonicalizeFlagValues(flagValues);
        foreach (var (flagName, control) in controls.GeneratedControls)
        {
            var primary = LlamaServerFlagSchema.FindByName(flagName)?.PrimaryName ?? flagName;
            if (canonical.TryGetValue(primary, out var value))
                LaunchSettingsControlFactory.SetControlValue(control, value);
        }
    }

    private static LlamaServerLaunchOptions BuildLaunchOptions(AppSettings settings, RuntimeBackend backend, IReadOnlyList<string>? flagOrder = null)
        => LlamaServerLaunchOptions.From(settings, backend, flagOrder);

    private static string ComboValue(WpfComboBox? combo)
        => (combo?.SelectedItem?.ToString() ?? combo?.Text ?? "").Trim().ToLowerInvariant();

    private static int ReadContextSize(WpfTextBox? box)
        => LaunchSettingParser.ReadContextSize(box?.Text.Trim() ?? "");

    private static int ReadInt(WpfTextBox? box, string label, int min, int? max = null)
        => LaunchSettingParser.ReadInt(box?.Text.Trim() ?? "", label, min, max);

    private static double ReadDouble(WpfTextBox? box, string label, double min, double? max = null)
        => LaunchSettingParser.ReadDouble(box?.Text.Trim() ?? "", label, min, max);

    public static (AppSettings settings, IReadOnlyList<string> errors) TryReadForPreview(AppSettings baseSettings, LaunchSettingsFormControls controls, bool treatEmptyAsDefault = true)
    {
        var errors = new List<string>();
        var defaults = AppSettings.CreateDefault(baseSettings.WorkspaceRoot);
        var next = TryReadControls(baseSettings, defaults, controls, errors, treatEmptyAsDefault);
        next = next with { FlagValues = ReadGeneratedFlagValues(controls, next.FlagValues) };
        errors.AddRange(LocalLlmConsole.Services.LaunchCommandValidator
            .Validate(next.FlagValues, validateFilePaths: false)
            .Errors);
        return (next, errors);
    }

    private static AppSettings TryReadControls(AppSettings baseSettings, AppSettings defaults, LaunchSettingsFormControls controls, List<string> errors, bool treatEmptyAsDefault)
        => baseSettings with
        {
            Port = TryReadIntBox(controls.LaunchPortBox, "Port", 1, 65535, defaults.Port, errors, treatEmptyAsDefault),
            ContextSize = TryReadContextSizeBox(controls.ContextSizeBox, defaults.ContextSize, errors, treatEmptyAsDefault),
            GpuLayers = TryReadIntBox(controls.GpuLayersBox, "GPU layers", 0, null, defaults.GpuLayers, errors, treatEmptyAsDefault),
            ParallelSlots = TryReadIntBox(controls.ParallelSlotsBox, "Parallel slots", 1, null, defaults.ParallelSlots, errors, treatEmptyAsDefault),
            BatchSize = TryReadIntBox(controls.BatchSizeBox, "Batch size", 1, null, defaults.BatchSize, errors, treatEmptyAsDefault),
            MicroBatchSize = TryReadIntBox(controls.MicroBatchSizeBox, "Micro batch size", 1, null, defaults.MicroBatchSize, errors, treatEmptyAsDefault),
            Threads = TryReadIntBox(controls.ThreadsBox, "Threads", 0, null, defaults.Threads, errors, treatEmptyAsDefault),
            ReasoningMode = TryReadComboValue(controls.ReasoningCombo, defaults.ReasoningMode),
            ReasoningFormat = TryReadComboValue(controls.ReasoningFormatCombo, defaults.ReasoningFormat),
            ReasoningBudget = TryReadIntBox(controls.ReasoningBudgetBox, "Reasoning budget", -1, null, defaults.ReasoningBudget, errors, treatEmptyAsDefault),
            VisionMode = TryReadComboValue(controls.VisionCombo, defaults.VisionMode),
            VisionProjectorPath = controls.VisionProjectorPathBox?.Text.Trim() ?? "",
            VisionImageMinTokens = TryReadIntBox(controls.VisionImageMinTokensBox, "Image min tokens", 0, null, defaults.VisionImageMinTokens, errors, treatEmptyAsDefault),
            VisionImageMaxTokens = TryReadIntBox(controls.VisionImageMaxTokensBox, "Image max tokens", 0, null, defaults.VisionImageMaxTokens, errors, treatEmptyAsDefault),
            FlashAttention = TryReadComboValue(controls.FlashAttentionCombo, defaults.FlashAttention),
            CacheTypeK = TryReadComboValue(controls.CacheTypeKCombo, defaults.CacheTypeK),
            CacheTypeV = TryReadComboValue(controls.CacheTypeVCombo, defaults.CacheTypeV),
            KvOffload = TryReadComboValue(controls.KvOffloadCombo, defaults.KvOffload),
            KvUnified = TryReadComboValue(controls.KvUnifiedCombo, defaults.KvUnified),
            PromptCacheMode = TryReadComboValue(controls.PromptCacheCombo, defaults.PromptCacheMode),
            PromptCacheRamMb = TryReadIntBox(controls.PromptCacheRamMbBox, "Prompt cache MB", -1, null, defaults.PromptCacheRamMb, errors, treatEmptyAsDefault),
            ContextCheckpointsMode = TryReadComboValue(controls.ContextCheckpointsCombo, defaults.ContextCheckpointsMode),
            ContextCheckpointCount = TryReadIntBox(controls.ContextCheckpointCountBox, "Checkpoint count", 0, null, defaults.ContextCheckpointCount, errors, treatEmptyAsDefault),
            ContextCheckpointEveryNTokens = TryReadIntBox(controls.ContextCheckpointEveryNTokensBox, "Checkpoint spacing", -1, null, defaults.ContextCheckpointEveryNTokens, errors, treatEmptyAsDefault),
            ContinuousBatching = TryReadComboValue(controls.ContinuousBatchingCombo, defaults.ContinuousBatching),
            JinjaMode = TryReadComboValue(controls.JinjaCombo, defaults.JinjaMode),
            MmapMode = TryReadComboValue(controls.MmapCombo, defaults.MmapMode),
            MlockMode = TryReadComboValue(controls.MlockCombo, defaults.MlockMode),
            EnableMetrics = TryReadBoolComboValue(controls.MetricsCombo, defaults.EnableMetrics),
            Temperature = TryReadDoubleBox(controls.TemperatureBox, "Temperature", 0, null, defaults.Temperature, errors, treatEmptyAsDefault),
            TopK = TryReadIntBox(controls.TopKBox, "Top K", 0, null, defaults.TopK, errors, treatEmptyAsDefault),
            TopP = TryReadDoubleBox(controls.TopPBox, "Top P", 0, 1, defaults.TopP, errors, treatEmptyAsDefault),
            MinP = TryReadDoubleBox(controls.MinPBox, "Min P", 0, 1, defaults.MinP, errors, treatEmptyAsDefault),
            MaxTokens = TryReadIntBox(controls.MaxTokensBox, "Max tokens", -1, null, defaults.MaxTokens, errors, treatEmptyAsDefault),
            Seed = TryReadIntBox(controls.SeedBox, "Seed", -1, null, defaults.Seed, errors, treatEmptyAsDefault),
            RepeatLastN = TryReadIntBox(controls.RepeatLastNBox, "Repeat window", -1, null, defaults.RepeatLastN, errors, treatEmptyAsDefault),
            RepeatPenalty = TryReadDoubleBox(controls.RepeatPenaltyBox, "Repeat penalty", 0, null, defaults.RepeatPenalty, errors, treatEmptyAsDefault),
            PresencePenalty = TryReadDoubleBox(controls.PresencePenaltyBox, "Presence penalty", -10, 10, defaults.PresencePenalty, errors, treatEmptyAsDefault),
            FrequencyPenalty = TryReadDoubleBox(controls.FrequencyPenaltyBox, "Frequency penalty", -10, 10, defaults.FrequencyPenalty, errors, treatEmptyAsDefault),
            RopeScaling = TryReadComboValue(controls.RopeScalingCombo, defaults.RopeScaling),
            RopeScale = TryReadDoubleBox(controls.RopeScaleBox, "RoPE scale", 0, null, defaults.RopeScale, errors, treatEmptyAsDefault),
            RopeFreqBase = TryReadDoubleBox(controls.RopeFreqBaseBox, "RoPE base", 0, null, defaults.RopeFreqBase, errors, treatEmptyAsDefault),
            RopeFreqScale = TryReadDoubleBox(controls.RopeFreqScaleBox, "RoPE frequency scale", 0, null, defaults.RopeFreqScale, errors, treatEmptyAsDefault),
            SpeculativeType = TryReadComboValue(controls.SpeculativeTypeCombo, defaults.SpeculativeType),
            SpecDraftModelPath = controls.SpecDraftModelPathBox?.Text.Trim() ?? "",
            MtpHeadPath = controls.MtpHeadPathBox?.Text.Trim() ?? "",
            SpecDraftGpuLayers = TryReadIntBox(controls.SpecDraftGpuLayersBox, "Draft GPU layers", -1, null, defaults.SpecDraftGpuLayers, errors, treatEmptyAsDefault),
            SpecDraftMinTokens = TryReadIntBox(controls.SpecDraftMinTokensBox, "Draft min tokens", 0, null, defaults.SpecDraftMinTokens, errors, treatEmptyAsDefault),
            SpecDraftMaxTokens = TryReadIntBox(controls.SpecDraftMaxTokensBox, "Draft max tokens", 0, null, defaults.SpecDraftMaxTokens, errors, treatEmptyAsDefault),
            SpecDraftPSplit = TryReadDoubleBox(controls.SpecDraftPSplitBox, "Draft split probability", -1, 1, defaults.SpecDraftPSplit, errors, treatEmptyAsDefault),
            SpecDraftPMin = TryReadDoubleBox(controls.SpecDraftPMinBox, "Draft min probability", -1, 1, defaults.SpecDraftPMin, errors, treatEmptyAsDefault),
            SpecDraftCacheTypeK = TryReadComboValue(controls.SpecDraftCacheTypeKCombo, defaults.SpecDraftCacheTypeK),
            SpecDraftCacheTypeV = TryReadComboValue(controls.SpecDraftCacheTypeVCombo, defaults.SpecDraftCacheTypeV),
            CustomParameters = controls.CustomParametersBox?.Text.Trim() ?? ""
        };

    private static int TryReadIntBox(WpfTextBox? box, string label, int min, int? max, int baseValue, List<string> errors, bool treatEmptyAsDefault)
    {
        var text = box?.Text.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(text))
        {
            if (!treatEmptyAsDefault)
                errors.Add($"{label} must be a whole number.");
            return baseValue;
        }

        if (LaunchSettingParser.TryReadInt(text, label, min, max, out var value, out var error))
            return value;

        errors.Add(error!);
        return baseValue;
    }

    private static double TryReadDoubleBox(WpfTextBox? box, string label, double min, double? max, double baseValue, List<string> errors, bool treatEmptyAsDefault)
    {
        var text = box?.Text.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(text))
        {
            if (!treatEmptyAsDefault)
                errors.Add($"{label} must be a number.");
            return baseValue;
        }

        if (LaunchSettingParser.TryReadDouble(text, label, min, max, out var value, out var error))
            return value;

        errors.Add(error!);
        return baseValue;
    }

    private static int TryReadContextSizeBox(WpfTextBox? box, int baseValue, List<string> errors, bool treatEmptyAsDefault)
    {
        var text = box?.Text.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(text))
        {
            if (!treatEmptyAsDefault)
                errors.Add("Context size must be 0, a token count, or shorthand like 196k.");
            return baseValue;
        }

        if (LaunchSettingParser.TryReadContextSize(text, out var value, out var error))
            return value;

        errors.Add(error!);
        return baseValue;
    }

    private static string TryReadComboValue(WpfComboBox? combo, string baseValue)
    {
        var text = ComboValue(combo);
        return string.IsNullOrWhiteSpace(text) ? baseValue : text;
    }

    private static bool TryReadBoolComboValue(WpfComboBox? combo, bool baseValue)
    {
        var text = ComboValue(combo);
        if (string.IsNullOrWhiteSpace(text))
            return baseValue;
        return string.Equals(text, "on", StringComparison.OrdinalIgnoreCase);
    }
}
