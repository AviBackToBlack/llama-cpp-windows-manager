using System.Windows;
using System.Windows.Controls;
using LocalLlmConsole.Services;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace LocalLlmConsole;

public static partial class LaunchSettingsPanelFactory
{
    private static readonly string[] BasicFlagNames =
    [
        "--ctx-size", "--threads", "--gpu-layers", "--n-gpu-layers",
        "--batch-size", "--ubatch-size", "--flash-attn", "--cache-type-k", "--cache-type-v",
        "--reasoning", "--reasoning-format", "--reasoning-budget",
        "--jinja", "--vision", "--vision-head", "--image-min-tokens", "--image-max-tokens",
        "--temp", "--top-k", "--top-p", "--min-p",
        "--parallel", "--cont-batching", "--metrics"
    ];

    private static LaunchSettingsFormControls AddLaunchSections(StackPanel panel, LaunchSettingsPanelBuilder builder, LaunchSettingsPanelRequest request, WpfTextBox launchPortBox)
    {
        var settings = request.Settings;
        var formControls = new LaunchSettingsFormControls { LaunchPortBox = launchPortBox };
        var generatedControls = new Dictionary<string, FrameworkElement>(StringComparer.OrdinalIgnoreCase);
        var excludedFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var commandLineBox = new WpfTextBox
        {
            MinHeight = 60,
            MinWidth = 72,
            Margin = new Thickness(0, 0, 4, 2),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            ToolTip = Loc.T("Tooltip.Field.CommandLine")
        };
        formControls.CommandPreviewBox = commandLineBox;
        var commandLineGrid = LaunchSettingsGrid();
        builder.AddLaunchSetting(commandLineGrid, Loc.T("Launch.Field.CommandLine"), commandLineBox);
        commandLineBox.ToolTip = Loc.T("Tooltip.Field.CommandLine");
        AddLaunchSection(panel, builder, Loc.T("Launch.Section.CommandLine"), commandLineGrid);

        var basicGrid = LaunchSettingsGrid();
        formControls.ContextSizeBox = AddFirstClassTextBox(basicGrid, builder, Loc.T("Launch.Field.ContextSize"), "--ctx-size", settings.ContextSize, excludedFlags);
        formControls.ThreadsBox = AddFirstClassTextBox(basicGrid, builder, Loc.T("Launch.Field.Threads"), "--threads", settings.Threads, excludedFlags);
        formControls.GpuLayersBox = AddFirstClassTextBox(basicGrid, builder, Loc.T("Launch.Field.GpuLayers"), "--gpu-layers", settings.GpuLayers, excludedFlags);
        AddGeneratedFlags(basicGrid, builder, "Basic", excludedFlags, generatedControls, formControls);
        AddLaunchSection(panel, builder, Loc.T("Launch.Section.Basic"), basicGrid);

        var memoryGrid = LaunchSettingsGrid();
        var memoryControls = AddPerformanceMemorySettings(memoryGrid, builder, settings, excludedFlags);
        AddGeneratedFlags(memoryGrid, builder, "Memory", excludedFlags, generatedControls, formControls, skip: f => IsContextExtensionFlag(f));
        AddLaunchSection(panel, builder, Loc.T("Launch.Section.PerformanceMemory"), memoryGrid);

        var ropeGrid = LaunchSettingsGrid();
        formControls.RopeScalingCombo = AddFirstClassCombo(ropeGrid, builder, Loc.T("Launch.Field.RopeScaling"), "--rope-scaling", settings.RopeScaling, LaunchSettingMetadataService.RopeScalingOptions, excludedFlags);
        formControls.RopeScaleBox = AddFirstClassTextBox(ropeGrid, builder, Loc.T("Launch.Field.RopeScale"), "--rope-scale", settings.RopeScale, excludedFlags);
        formControls.RopeFreqBaseBox = AddFirstClassTextBox(ropeGrid, builder, Loc.T("Launch.Field.RopeBase"), "--rope-freq-base", settings.RopeFreqBase, excludedFlags);
        formControls.RopeFreqScaleBox = AddFirstClassTextBox(ropeGrid, builder, Loc.T("Launch.Field.RopeFreq"), "--rope-freq-scale", settings.RopeFreqScale, excludedFlags);
        AddGeneratedFlags(ropeGrid, builder, "Memory", excludedFlags, generatedControls, formControls, include: f => IsContextExtensionFlag(f));
        AddLaunchSection(panel, builder, Loc.T("Launch.Section.ContextExtension"), ropeGrid, isAdvancedSection: true);

        var speculativeGrid = LaunchSettingsGrid();
        formControls.SpeculativeTypeCombo = AddFirstClassCombo(speculativeGrid, builder, Loc.T("Launch.Field.SpecType"), "--spec-type", LaunchSettingMetadataService.NormalizeSpeculativeType(settings.SpeculativeType), LaunchSettingMetadataService.SpeculativeTypeOptions, excludedFlags);
        formControls.SpecDraftModelPathBox = AddFirstClassTextBox(speculativeGrid, builder, Loc.T("Launch.Field.DraftModel"), "--model-draft", settings.SpecDraftModelPath, excludedFlags);
        var mtpHeadPathBox = LaunchTextBox(settings.MtpHeadPath);
        var mtpHeadPicker = MtpHeadPicker(mtpHeadPathBox, request.ChooseMtpHeadAsync, out var mtpHeadButton);
        AddFirstClassControl(speculativeGrid, builder, Loc.T("Launch.Field.MtpHead"), "--mtp-head", mtpHeadPicker, excludedFlags);
        formControls.MtpHeadPathBox = mtpHeadPathBox;
        formControls.MtpHeadButton = mtpHeadButton;
        formControls.SpecDraftCacheTypeKCombo = AddFirstClassCombo(speculativeGrid, builder, Loc.T("Launch.Field.DraftKCache"), "--cache-type-k-draft", settings.SpecDraftCacheTypeK, LaunchSettingMetadataService.CacheTypeOptions, excludedFlags);
        formControls.SpecDraftCacheTypeVCombo = AddFirstClassCombo(speculativeGrid, builder, Loc.T("Launch.Field.DraftVCache"), "--cache-type-v-draft", settings.SpecDraftCacheTypeV, LaunchSettingMetadataService.CacheTypeOptions, excludedFlags);
        formControls.SpecDraftMaxTokensBox = AddFirstClassTextBox(speculativeGrid, builder, Loc.T("Launch.Field.DraftMax"), "--spec-draft-n-max", settings.SpecDraftMaxTokens, excludedFlags);
        formControls.SpecDraftMinTokensBox = AddFirstClassTextBox(speculativeGrid, builder, Loc.T("Launch.Field.DraftMin"), "--spec-draft-n-min", settings.SpecDraftMinTokens, excludedFlags);
        formControls.SpecDraftGpuLayersBox = AddFirstClassTextBox(speculativeGrid, builder, Loc.T("Launch.Field.DraftGpu"), "--spec-draft-ngl", settings.SpecDraftGpuLayers, excludedFlags, advanced: true);
        formControls.SpecDraftPSplitBox = AddFirstClassTextBox(speculativeGrid, builder, Loc.T("Launch.Field.SplitProb"), "--spec-draft-p-split", settings.SpecDraftPSplit, excludedFlags, advanced: true);
        formControls.SpecDraftPMinBox = AddFirstClassTextBox(speculativeGrid, builder, Loc.T("Launch.Field.MinProb"), "--spec-draft-p-min", settings.SpecDraftPMin, excludedFlags, advanced: true);
        AddGeneratedFlags(speculativeGrid, builder, "Speculative", excludedFlags, generatedControls, formControls);
        AddLaunchSection(panel, builder, Loc.T("Launch.Section.SpeculativeMtp"), speculativeGrid);

        var chatGrid = LaunchSettingsGrid();
        formControls.ReasoningCombo = AddFirstClassCombo(chatGrid, builder, Loc.T("Launch.Field.Reasoning"), "--reasoning", settings.ReasoningMode, LaunchSettingMetadataService.AutoOnOffOptions, excludedFlags);
        formControls.ReasoningFormatCombo = AddFirstClassCombo(chatGrid, builder, Loc.T("Launch.Field.ReasonFormat"), "--reasoning-format", settings.ReasoningFormat, LaunchSettingMetadataService.ReasoningFormatOptions, excludedFlags);
        formControls.ReasoningBudgetBox = AddFirstClassTextBox(chatGrid, builder, Loc.T("Launch.Field.ReasonBudget"), "--reasoning-budget", settings.ReasoningBudget, excludedFlags);
        formControls.JinjaCombo = AddFirstClassCombo(chatGrid, builder, Loc.T("Launch.Field.JinjaChat"), "--jinja", settings.JinjaMode, LaunchSettingMetadataService.AutoOnOffOptions, excludedFlags);
        formControls.VisionCombo = AddFirstClassCombo(chatGrid, builder, Loc.T("Launch.Field.Vision"), "--mmproj-auto", settings.VisionMode, LaunchSettingMetadataService.AutoOnOffOptions, excludedFlags);
        var visionProjectorPathBox = LaunchTextBox(settings.VisionProjectorPath);
        var visionProjectorPicker = VisionProjectorPicker(visionProjectorPathBox, request.ChooseVisionProjectorAsync, out var visionProjectorButton);
        AddFirstClassControl(chatGrid, builder, Loc.T("Launch.Field.VisionHead"), "--mmproj", visionProjectorPicker, excludedFlags);
        formControls.VisionProjectorPathBox = visionProjectorPathBox;
        formControls.VisionProjectorButton = visionProjectorButton;
        formControls.VisionImageMinTokensBox = AddFirstClassTextBox(chatGrid, builder, Loc.T("Launch.Field.ImageMin"), "--image-min-tokens", settings.VisionImageMinTokens, excludedFlags);
        formControls.VisionImageMaxTokensBox = AddFirstClassTextBox(chatGrid, builder, Loc.T("Launch.Field.ImageMax"), "--image-max-tokens", settings.VisionImageMaxTokens, excludedFlags);
        AddGeneratedFlags(chatGrid, builder, "Server", excludedFlags, generatedControls, formControls, include: f => IsChatCapabilityFlag(f));
        AddGeneratedFlags(chatGrid, builder, "Vision", excludedFlags, generatedControls, formControls);
        AddLaunchSection(panel, builder, Loc.T("Launch.Section.ChatCapabilities"), chatGrid);

        var generationGrid = LaunchSettingsGrid();
        formControls.TemperatureBox = AddFirstClassTextBox(generationGrid, builder, Loc.T("Launch.Field.Temperature"), "--temp", settings.Temperature, excludedFlags);
        formControls.TopKBox = AddFirstClassTextBox(generationGrid, builder, Loc.T("Launch.Field.TopK"), "--top-k", settings.TopK, excludedFlags);
        formControls.TopPBox = AddFirstClassTextBox(generationGrid, builder, Loc.T("Launch.Field.TopP"), "--top-p", settings.TopP, excludedFlags);
        formControls.MinPBox = AddFirstClassTextBox(generationGrid, builder, Loc.T("Launch.Field.MinP"), "--min-p", settings.MinP, excludedFlags);
        formControls.MaxTokensBox = AddFirstClassTextBox(generationGrid, builder, Loc.T("Launch.Field.MaxTokens"), "--predict", settings.MaxTokens, excludedFlags, advanced: true);
        formControls.SeedBox = AddFirstClassTextBox(generationGrid, builder, Loc.T("Launch.Field.Seed"), "--seed", settings.Seed, excludedFlags, advanced: true);
        formControls.RepeatLastNBox = AddFirstClassTextBox(generationGrid, builder, Loc.T("Launch.Field.RepeatWindow"), "--repeat-last-n", settings.RepeatLastN, excludedFlags, advanced: true);
        formControls.RepeatPenaltyBox = AddFirstClassTextBox(generationGrid, builder, Loc.T("Launch.Field.RepeatPen"), "--repeat-penalty", settings.RepeatPenalty, excludedFlags, advanced: true);
        formControls.PresencePenaltyBox = AddFirstClassTextBox(generationGrid, builder, Loc.T("Launch.Field.Presence"), "--presence-penalty", settings.PresencePenalty, excludedFlags, advanced: true);
        formControls.FrequencyPenaltyBox = AddFirstClassTextBox(generationGrid, builder, Loc.T("Launch.Field.Frequency"), "--frequency-penalty", settings.FrequencyPenalty, excludedFlags, advanced: true);
        AddGeneratedFlags(generationGrid, builder, "Sampling", excludedFlags, generatedControls, formControls);
        AddLaunchSection(panel, builder, Loc.T("Launch.Section.GenerationDefaults"), generationGrid);

        var serverGrid = LaunchSettingsGrid();
        formControls.ParallelSlotsBox = AddFirstClassTextBox(serverGrid, builder, Loc.T("Launch.Field.ParallelSlots"), "--parallel", settings.ParallelSlots, excludedFlags);
        formControls.ContinuousBatchingCombo = AddFirstClassCombo(serverGrid, builder, Loc.T("Launch.Field.ContinuousBatch"), "--cont-batching", settings.ContinuousBatching, LaunchSettingMetadataService.OnOffOptions, excludedFlags);
        formControls.MetricsCombo = AddFirstClassCombo(serverGrid, builder, Loc.T("Launch.Field.Metrics"), "--metrics", settings.EnableMetrics ? "on" : "off", LaunchSettingMetadataService.OnOffOptions, excludedFlags);
        AddGeneratedFlags(serverGrid, builder, "Server", excludedFlags, generatedControls, formControls, skip: f => IsChatCapabilityFlag(f));
        var customParametersBox = LaunchTextBox(settings.CustomParameters);
        AddFirstClassControl(serverGrid, builder, Loc.T("Launch.Field.CustomParams"), "--custom-params", customParametersBox, excludedFlags, advanced: true);
        formControls.CustomParametersBox = customParametersBox;
        AddLaunchSection(panel, builder, Loc.T("Launch.Section.Server"), serverGrid, isAdvancedSection: true);

        AddGeneratedSection(panel, builder, "Model", excludedFlags, generatedControls, formControls);
        AddGeneratedSection(panel, builder, "Logging", excludedFlags, generatedControls, formControls);

        formControls.GeneratedControls = generatedControls;

        return formControls;
    }

    private static void AddLaunchSection(
        StackPanel panel,
        LaunchSettingsPanelBuilder builder,
        string title,
        Grid grid,
        bool isAdvancedSection = false)
    {
        var section = LaunchSection(title, grid);
        builder.AddSection(title, section, grid, isAdvancedSection);
        if (isAdvancedSection)
            builder.AddAdvancedSection(section);
        panel.Children.Add(section);
    }
}
