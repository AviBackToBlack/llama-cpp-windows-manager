using System.Windows.Controls;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace LocalLlmConsole;

public static partial class LaunchSettingsPanelFactory
{
    private static LaunchSettingsFormControls AddLaunchSections(StackPanel panel, LaunchSettingsPanelBuilder builder, LaunchSettingsPanelRequest request, WpfTextBox launchPortBox)
    {
        var settings = request.Settings;
        var basicGrid = LaunchSettingsGrid();
        var contextSizeBox = LaunchTextBox(settings.ContextSize);
        builder.AddLaunchSetting(basicGrid, Loc.T("Launch.Field.ContextSize"), contextSizeBox);
        var threadsBox = LaunchTextBox(settings.Threads);
        builder.AddLaunchSetting(basicGrid, Loc.T("Launch.Field.Threads"), threadsBox);
        var gpuLayersBox = LaunchTextBox(settings.GpuLayers);
        builder.AddLaunchSetting(basicGrid, Loc.T("Launch.Field.GpuLayers"), gpuLayersBox);
        AddLaunchSection(panel, builder, Loc.T("Launch.Section.Basic"), basicGrid);

        var memoryGrid = LaunchSettingsGrid();
        var memoryControls = AddPerformanceMemorySettings(memoryGrid, builder, settings);
        AddLaunchSection(panel, builder, Loc.T("Launch.Section.PerformanceMemory"), memoryGrid);

        var speculativeGrid = LaunchSettingsGrid();
        var speculativeTypeCombo = LaunchCombo(LaunchSettingMetadataService.SpeculativeTypeOptions);
        builder.AddLaunchSetting(speculativeGrid, Loc.T("Launch.Field.SpecType"), speculativeTypeCombo);
        var specDraftModelPathBox = LaunchTextBox(settings.SpecDraftModelPath);
        builder.AddLaunchSetting(speculativeGrid, Loc.T("Launch.Field.DraftModel"), specDraftModelPathBox);
        var mtpHeadPathBox = LaunchTextBox(settings.MtpHeadPath);
        var mtpHeadPicker = MtpHeadPicker(mtpHeadPathBox, request.ChooseMtpHeadAsync, out var mtpHeadButton);
        builder.AddLaunchSetting(speculativeGrid, Loc.T("Launch.Field.MtpHead"), mtpHeadPicker);
        var specDraftCacheTypeKCombo = LaunchCombo(LaunchSettingMetadataService.CacheTypeOptions);
        builder.AddLaunchSetting(speculativeGrid, Loc.T("Launch.Field.DraftKCache"), specDraftCacheTypeKCombo);
        var specDraftCacheTypeVCombo = LaunchCombo(LaunchSettingMetadataService.CacheTypeOptions);
        builder.AddLaunchSetting(speculativeGrid, Loc.T("Launch.Field.DraftVCache"), specDraftCacheTypeVCombo);
        var specDraftMaxTokensBox = LaunchTextBox(settings.SpecDraftMaxTokens);
        builder.AddLaunchSetting(speculativeGrid, Loc.T("Launch.Field.DraftMax"), specDraftMaxTokensBox);
        var specDraftMinTokensBox = LaunchTextBox(settings.SpecDraftMinTokens);
        builder.AddLaunchSetting(speculativeGrid, Loc.T("Launch.Field.DraftMin"), specDraftMinTokensBox);
        var specDraftGpuLayersBox = LaunchTextBox(settings.SpecDraftGpuLayers);
        builder.AddAdvancedLaunchSetting(speculativeGrid, Loc.T("Launch.Field.DraftGpu"), specDraftGpuLayersBox);
        var specDraftPSplitBox = LaunchTextBox(settings.SpecDraftPSplit);
        builder.AddAdvancedLaunchSetting(speculativeGrid, Loc.T("Launch.Field.SplitProb"), specDraftPSplitBox);
        var specDraftPMinBox = LaunchTextBox(settings.SpecDraftPMin);
        builder.AddAdvancedLaunchSetting(speculativeGrid, Loc.T("Launch.Field.MinProb"), specDraftPMinBox);
        AddLaunchSection(panel, builder, Loc.T("Launch.Section.SpeculativeMtp"), speculativeGrid);

        var chatGrid = LaunchSettingsGrid();
        var reasoningCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        builder.AddLaunchSetting(chatGrid, Loc.T("Launch.Field.Reasoning"), reasoningCombo);
        var reasoningFormatCombo = LaunchCombo(LaunchSettingMetadataService.ReasoningFormatOptions);
        builder.AddLaunchSetting(chatGrid, Loc.T("Launch.Field.ReasonFormat"), reasoningFormatCombo);
        var reasoningBudgetBox = LaunchTextBox(settings.ReasoningBudget);
        builder.AddLaunchSetting(chatGrid, Loc.T("Launch.Field.ReasonBudget"), reasoningBudgetBox);
        var jinjaCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        builder.AddLaunchSetting(chatGrid, Loc.T("Launch.Field.JinjaChat"), jinjaCombo);
        var visionCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        builder.AddLaunchSetting(chatGrid, Loc.T("Launch.Field.Vision"), visionCombo);
        var visionProjectorPathBox = LaunchTextBox(settings.VisionProjectorPath);
        var visionProjectorPicker = VisionProjectorPicker(visionProjectorPathBox, request.ChooseVisionProjectorAsync, out var visionProjectorButton);
        builder.AddLaunchSetting(chatGrid, Loc.T("Launch.Field.VisionHead"), visionProjectorPicker);
        var visionImageMinTokensBox = LaunchTextBox(settings.VisionImageMinTokens);
        builder.AddLaunchSetting(chatGrid, Loc.T("Launch.Field.ImageMin"), visionImageMinTokensBox);
        var visionImageMaxTokensBox = LaunchTextBox(settings.VisionImageMaxTokens);
        builder.AddLaunchSetting(chatGrid, Loc.T("Launch.Field.ImageMax"), visionImageMaxTokensBox);
        AddLaunchSection(panel, builder, Loc.T("Launch.Section.ChatCapabilities"), chatGrid);

        var generationGrid = LaunchSettingsGrid();
        var temperatureBox = LaunchTextBox(settings.Temperature);
        builder.AddLaunchSetting(generationGrid, Loc.T("Launch.Field.Temperature"), temperatureBox);
        var topKBox = LaunchTextBox(settings.TopK);
        builder.AddLaunchSetting(generationGrid, Loc.T("Launch.Field.TopK"), topKBox);
        var topPBox = LaunchTextBox(settings.TopP);
        builder.AddLaunchSetting(generationGrid, Loc.T("Launch.Field.TopP"), topPBox);
        var minPBox = LaunchTextBox(settings.MinP);
        builder.AddLaunchSetting(generationGrid, Loc.T("Launch.Field.MinP"), minPBox);
        var maxTokensBox = LaunchTextBox(settings.MaxTokens);
        builder.AddAdvancedLaunchSetting(generationGrid, Loc.T("Launch.Field.MaxTokens"), maxTokensBox);
        var seedBox = LaunchTextBox(settings.Seed);
        builder.AddAdvancedLaunchSetting(generationGrid, Loc.T("Launch.Field.Seed"), seedBox);
        var repeatLastNBox = LaunchTextBox(settings.RepeatLastN);
        builder.AddAdvancedLaunchSetting(generationGrid, Loc.T("Launch.Field.RepeatWindow"), repeatLastNBox);
        var repeatPenaltyBox = LaunchTextBox(settings.RepeatPenalty);
        builder.AddAdvancedLaunchSetting(generationGrid, Loc.T("Launch.Field.RepeatPen"), repeatPenaltyBox);
        var presencePenaltyBox = LaunchTextBox(settings.PresencePenalty);
        builder.AddAdvancedLaunchSetting(generationGrid, Loc.T("Launch.Field.Presence"), presencePenaltyBox);
        var frequencyPenaltyBox = LaunchTextBox(settings.FrequencyPenalty);
        builder.AddAdvancedLaunchSetting(generationGrid, Loc.T("Launch.Field.Frequency"), frequencyPenaltyBox);
        AddLaunchSection(panel, builder, Loc.T("Launch.Section.GenerationDefaults"), generationGrid);

        var ropeGrid = LaunchSettingsGrid();
        var ropeScalingCombo = LaunchCombo(LaunchSettingMetadataService.RopeScalingOptions);
        builder.AddLaunchSetting(ropeGrid, Loc.T("Launch.Field.RopeScaling"), ropeScalingCombo);
        var ropeScaleBox = LaunchTextBox(settings.RopeScale);
        builder.AddLaunchSetting(ropeGrid, Loc.T("Launch.Field.RopeScale"), ropeScaleBox);
        var ropeFreqBaseBox = LaunchTextBox(settings.RopeFreqBase);
        builder.AddLaunchSetting(ropeGrid, Loc.T("Launch.Field.RopeBase"), ropeFreqBaseBox);
        var ropeFreqScaleBox = LaunchTextBox(settings.RopeFreqScale);
        builder.AddLaunchSetting(ropeGrid, Loc.T("Launch.Field.RopeFreq"), ropeFreqScaleBox);
        AddLaunchSection(panel, builder, Loc.T("Launch.Section.ContextExtension"), ropeGrid, isAdvancedSection: true);

        var serverGrid = LaunchSettingsGrid();
        var parallelSlotsBox = LaunchTextBox(settings.ParallelSlots);
        builder.AddLaunchSetting(serverGrid, Loc.T("Launch.Field.ParallelSlots"), parallelSlotsBox);
        var continuousBatchingCombo = LaunchCombo(LaunchSettingMetadataService.OnOffOptions);
        builder.AddLaunchSetting(serverGrid, Loc.T("Launch.Field.ContinuousBatch"), continuousBatchingCombo);
        var metricsCombo = LaunchCombo(LaunchSettingMetadataService.OnOffOptions);
        builder.AddLaunchSetting(serverGrid, Loc.T("Launch.Field.Metrics"), metricsCombo);
        var customParametersBox = LaunchTextBox(settings.CustomParameters);
        builder.AddAdvancedLaunchSetting(serverGrid, Loc.T("Launch.Field.CustomParams"), customParametersBox);
        AddLaunchSection(panel, builder, Loc.T("Launch.Section.Server"), serverGrid, isAdvancedSection: true);

        return new LaunchSettingsFormControls
        {
            LaunchPortBox = launchPortBox,
            ContextSizeBox = contextSizeBox,
            GpuLayersBox = gpuLayersBox,
            ParallelSlotsBox = parallelSlotsBox,
            BatchSizeBox = memoryControls.BatchSizeBox,
            MicroBatchSizeBox = memoryControls.MicroBatchSizeBox,
            ThreadsBox = threadsBox,
            ReasoningBudgetBox = reasoningBudgetBox,
            VisionProjectorPathBox = visionProjectorPathBox,
            VisionImageMinTokensBox = visionImageMinTokensBox,
            VisionImageMaxTokensBox = visionImageMaxTokensBox,
            TemperatureBox = temperatureBox,
            TopKBox = topKBox,
            TopPBox = topPBox,
            MinPBox = minPBox,
            MaxTokensBox = maxTokensBox,
            SeedBox = seedBox,
            RepeatLastNBox = repeatLastNBox,
            RepeatPenaltyBox = repeatPenaltyBox,
            PresencePenaltyBox = presencePenaltyBox,
            FrequencyPenaltyBox = frequencyPenaltyBox,
            RopeScaleBox = ropeScaleBox,
            RopeFreqBaseBox = ropeFreqBaseBox,
            RopeFreqScaleBox = ropeFreqScaleBox,
            SpecDraftModelPathBox = specDraftModelPathBox,
            MtpHeadPathBox = mtpHeadPathBox,
            MtpHeadButton = mtpHeadButton,
            SpecDraftGpuLayersBox = specDraftGpuLayersBox,
            SpecDraftMinTokensBox = specDraftMinTokensBox,
            SpecDraftMaxTokensBox = specDraftMaxTokensBox,
            SpecDraftPSplitBox = specDraftPSplitBox,
            SpecDraftPMinBox = specDraftPMinBox,
            MetricsCombo = metricsCombo,
            ReasoningCombo = reasoningCombo,
            ReasoningFormatCombo = reasoningFormatCombo,
            VisionCombo = visionCombo,
            VisionProjectorButton = visionProjectorButton,
            FlashAttentionCombo = memoryControls.FlashAttentionCombo,
            CacheTypeKCombo = memoryControls.CacheTypeKCombo,
            CacheTypeVCombo = memoryControls.CacheTypeVCombo,
            KvOffloadCombo = memoryControls.KvOffloadCombo,
            KvUnifiedCombo = memoryControls.KvUnifiedCombo,
            PromptCacheCombo = memoryControls.PromptCacheCombo,
            PromptCacheRamMbBox = memoryControls.PromptCacheRamMbBox,
            ContextCheckpointsCombo = memoryControls.ContextCheckpointsCombo,
            ContextCheckpointCountBox = memoryControls.ContextCheckpointCountBox,
            ContextCheckpointEveryNTokensBox = memoryControls.ContextCheckpointEveryNTokensBox,
            ContinuousBatchingCombo = continuousBatchingCombo,
            JinjaCombo = jinjaCombo,
            MmapCombo = memoryControls.MmapCombo,
            MlockCombo = memoryControls.MlockCombo,
            RopeScalingCombo = ropeScalingCombo,
            SpeculativeTypeCombo = speculativeTypeCombo,
            SpecDraftCacheTypeKCombo = specDraftCacheTypeKCombo,
            SpecDraftCacheTypeVCombo = specDraftCacheTypeVCombo,
            CustomParametersBox = customParametersBox
        };
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
