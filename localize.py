#!/usr/bin/env python3
"""Bulk localization: replaces hardcoded English UI strings with Loc.T("Key") calls."""
import os, re

BASE = r"D:\LLM\LocalLlmConsole\src\LocalLlmConsole.App"

def process(path, replacements):
    full = os.path.join(BASE, path)
    with open(full, 'r', encoding='utf-8') as f:
        content = f.read()
    count = 0
    for old, new in replacements:
        if old in content:
            content = content.replace(old, new)
            count += 1
        else:
            print(f"  MISSED in {path}: [{old[:60]}...]")
    if count > 0:
        with open(full, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"  {path}: {count} replacements")

# ====== OverviewPageFactory.cs ======
process("Ui/Pages/Overview/OverviewPageFactory.cs", [
    ('public const string LoadedSessionsTitle = "Loaded Model Sessions";',
     'public const string LoadedSessionsTitle = Loc.T("Overview.LoadedSessionsTitle");'),
    ('public const string LiveRuntimeLogTitle = "Live Runtime Log";',
     'public const string LiveRuntimeLogTitle = Loc.T("Overview.LiveRuntimeLogTitle");'),
    ('public const string RuntimeMetricsTitle = "All llama.cpp Metrics";',
     'public const string RuntimeMetricsTitle = Loc.T("Overview.RuntimeMetricsTitle");'),

    ('        ("Model", "C1", 1.45),\n        ("Size", "C2", .62),\n        ("State", "C3", .62),\n        ("API endpoints", "C4", 1.9),\n        ("Runtime", "C5", 1.25),\n        ("Backend", "C6", .75)',
     '        (Loc.T("Overview.SessionsCol.Model"), "C1", 1.45),\n        (Loc.T("Overview.SessionsCol.Size"), "C2", .62),\n        (Loc.T("Overview.SessionsCol.State"), "C3", .62),\n        (Loc.T("Overview.SessionsCol.ApiEndpoints"), "C4", 1.9),\n        (Loc.T("Overview.SessionsCol.Runtime"), "C5", 1.25),\n        (Loc.T("Overview.SessionsCol.Backend"), "C6", .75)'),

    ('        ("Metric", "C1", 1.5),\n        ("Labels", "C2", 2.2),\n        ("Value", "C3", .9),\n        ("Type", "C4", .7),\n        ("Help", "C5", 3)',
     '        (Loc.T("Overview.MetricsCol.Metric"), "C1", 1.5),\n        (Loc.T("Overview.MetricsCol.Labels"), "C2", 2.2),\n        (Loc.T("Overview.MetricsCol.Value"), "C3", .9),\n        (Loc.T("Overview.MetricsCol.Type"), "C4", .7),\n        (Loc.T("Overview.MetricsCol.Help"), "C5", 3)'),

    ('            Text = "Model",\n            FontWeight = FontWeights.SemiBold,\n            Foreground',
     '            Text = Loc.T("Overview.ModelLabel"),\n            FontWeight = FontWeights.SemiBold,\n            Foreground'),

    ('            ToolTip = "Choose a local model to load with its saved launch profile."',
     '            ToolTip = Loc.T("Tooltip.OverviewModelCombo")'),

    ('        loadButton = Button("Load", request.Actions.LoadSelectedModelAsync);',
     '        loadButton = Button(Loc.T("Overview.LoadButton"), request.Actions.LoadSelectedModelAsync);'),
    ('        unloadButton = Button("Unload", request.Actions.UnloadSelectedModelAsync);',
     '        unloadButton = Button(Loc.T("Overview.UnloadButton"), request.Actions.UnloadSelectedModelAsync);'),

    ('        model = MetricCardFactory.AddMetric(runtimeDashboard, "Model status", 0, 0);',
     '        model = MetricCardFactory.AddMetric(runtimeDashboard, Loc.T("Overview.Metric.ModelStatus"), 0, 0);'),
    ('        gpu = MetricCardFactory.AddMetric(runtimeDashboard, "Hardware", 0, 1);',
     '        gpu = MetricCardFactory.AddMetric(runtimeDashboard, Loc.T("Overview.Metric.Hardware"), 0, 1);'),
    ('        requests = MetricCardFactory.AddMetric(runtimeDashboard, "Settings", 0, 2);',
     '        requests = MetricCardFactory.AddMetric(runtimeDashboard, Loc.T("Overview.Metric.Settings"), 0, 2);'),
    ('        tokens = MetricCardFactory.AddMetric(runtimeDashboard, "Tokens", 1, 0, out tokensLastKnown);',
     '        tokens = MetricCardFactory.AddMetric(runtimeDashboard, Loc.T("Overview.Metric.Tokens"), 1, 0, out tokensLastKnown);'),
    ('        mtpTokens = MetricCardFactory.AddMetric(runtimeDashboard, "MTP tokens", 1, 1);',
     '        mtpTokens = MetricCardFactory.AddMetric(runtimeDashboard, Loc.T("Overview.Metric.MtpTokens"), 1, 1);'),
    ('        slots = MetricCardFactory.AddMetric(runtimeDashboard, "Slots", 1, 2);',
     '        slots = MetricCardFactory.AddMetric(runtimeDashboard, Loc.T("Overview.Metric.Slots"), 1, 2);'),

    ('            Text = "No runtime log is active.",',
     '            Text = Loc.T("Overview.NoRuntimeLog"),'),

    ('        dashboardSection.Children.Add(Text("Model Status", 18, true));',
     '        dashboardSection.Children.Add(Text(Loc.T("Overview.ModelStatusLabel"), 18, true));'),

    ('            "Load" => "Load the selected model with its saved launch settings.",\n            "Unload" => "Stop the currently loading or loaded model and free runtime resources."',
     '            Loc.T("Overview.LoadButton") => Loc.T("Tooltip.Load"),\n            Loc.T("Overview.UnloadButton") => Loc.T("Tooltip.Unload"),'),
])

# ====== MetricCardFactory.cs ======
process("Ui/Common/MetricCardFactory.cs", [
    ('        var age = now <= capturedAt ? "just now" : DisplayFormatService.Elapsed(now - capturedAt);',
     '        var age = now <= capturedAt ? Loc.T("Metrics.JustNow") : DisplayFormatService.Elapsed(now - capturedAt);'),
    ('        target.Text = $"Last known {age} ago";',
     '        target.Text = Loc.T("Metrics.LastKnownAgo", age);'),
    ('        target.ToolTip = "Live token rates are using the most recent successful metrics sample.";',
     '        target.ToolTip = Loc.T("Tooltip.MetricsLastKnown");'),
])

# ====== ModelsPageFactory.cs ======
process("Ui/Pages/Models/ModelsPageFactory.cs", [
    ('public const string ModelFilesTitle = "Model Files";',
     'public const string ModelFilesTitle = Loc.T("Models.ModelFilesTitle");'),
    ('public const string SavedModelVariantsTitle = "Saved Model Variants";',
     'public const string SavedModelVariantsTitle = Loc.T("Models.SavedVariantsTitle");'),
    ('public const string ModelFilesDescription = "Physical GGUF files discovered in the model folder or imported from another folder.";',
     'public const string ModelFilesDescription = Loc.T("Models.ModelFilesDescription");'),
    ('public const string SavedModelVariantsDescription = "Loadable aliases created from launch settings. They share the same GGUF file but keep separate names, model ids, ports, and profiles.";',
     'public const string SavedModelVariantsDescription = Loc.T("Models.SavedVariantsDescription");'),

    ('            "Models folder",',
     '            Loc.T("Models.FolderLabel"),'),
    ('            ("Scan Models Folder", request.Actions.ScanModelsFolderAsync),',
     '            (Loc.T("Models.ScanButton"), request.Actions.ScanModelsFolderAsync),'),

    ('        var modelsGrid = PageSectionFactory.GridFor(\n            ("Name", nameof(ModelGridRow.Name), 2.35),\n            ("Quant", nameof(ModelGridRow.Quant), .6),\n            ("Size", nameof(ModelGridRow.Size), .65));',
     '        var modelsGrid = PageSectionFactory.GridFor(\n            (Loc.T("Models.Col.Name"), nameof(ModelGridRow.Name), 2.35),\n            (Loc.T("Models.Col.Quant"), nameof(ModelGridRow.Quant), .6),\n            (Loc.T("Models.Col.Size"), nameof(ModelGridRow.Size), .65));'),

    ('        PageSectionFactory.AddButtonColumn(modelsGrid, "Open Folder",',
     '        PageSectionFactory.AddButtonColumn(modelsGrid, Loc.T("Models.ActionBtn.OpenFolder"),'),
    ('        PageSectionFactory.AddButtonColumn(modelsGrid, "Delete",',
     '        PageSectionFactory.AddButtonColumn(modelsGrid, Loc.T("Models.ActionBtn.Delete"),'),

    ('            ("Name", nameof(ModelGridRow.Name), 1.35),\n            ("Base model", nameof(ModelGridRow.BaseModel), 1.35),\n            ("Port", nameof(ModelGridRow.Port), .45));',
     '            (Loc.T("Models.Col.Name"), nameof(ModelGridRow.Name), 1.35),\n            (Loc.T("Models.Col.BaseModel"), nameof(ModelGridRow.BaseModel), 1.35),\n            (Loc.T("Models.Col.Port"), nameof(ModelGridRow.Port), .45));'),

    ('        PageSectionFactory.AddButtonColumn(modelVariantsGrid, "Open Folder",',
     '        PageSectionFactory.AddButtonColumn(modelVariantsGrid, Loc.T("Models.ActionBtn.OpenFolder"),'),
    ('        PageSectionFactory.AddButtonColumn(modelVariantsGrid, "Remove",',
     '        PageSectionFactory.AddButtonColumn(modelVariantsGrid, Loc.T("Models.ActionBtn.Remove"),'),

    ('            ToolTip = "Hugging Face search term, repo id, or model file URL"',
     '            ToolTip = Loc.T("Tooltip.HfSearchBox")'),

    ('"Scan Models Folder" => "Scan the models folder for local GGUF files.",',
     'Loc.T("Models.ScanButton") => Loc.T("Tooltip.ScanModelsFolder"),'),
])

# ====== LaunchSettingsPanelFactory.cs ======
process("Ui/Pages/Models/LaunchSettingsPanelFactory.cs", [
    ('        launchPortBox.ToolTip = TooltipText("Fixed server port for this model. Use a unique port per model when serving multiple models.");',
     '        launchPortBox.ToolTip = Loc.T("Tooltip.LaunchPortBox");'),
    ('        var modelCapabilityText = Text("No model selected", 12, false, true);',
     '        var modelCapabilityText = Text(Loc.T("Launch.NoModelSelected"), 12, false, true);'),
])

# ====== LaunchSettingsPanelFactory.Layout.cs ======
process("Ui/Pages/Models/LaunchSettingsPanelFactory.Layout.cs", [
    ('            "Save For Model" => "Save these launch settings for the selected model.",',
     '            Loc.T("Launch.SaveForModelButton") => Loc.T("Tooltip.SaveForModel"),'),
    ('            "Save As Default" => "Save these launch settings as the default for new models.",',
     '            Loc.T("Launch.SaveAsDefaultButton") => Loc.T("Tooltip.SaveAsDefault"),'),
    ('            "Reset Defaults" => "Restore launch settings to the app defaults.",',
     '            Loc.T("Launch.ResetDefaultsButton") => Loc.T("Tooltip.ResetDefaults"),'),
    ('            "Save As New" => "Save the current launch settings as a separate loadable model variant on a new direct API port.",',
     '            Loc.T("Launch.SaveAsNewButton") => Loc.T("Tooltip.SaveAsNewButton"),'),
])

# ====== LaunchSettingsPanelFactory.Controls.cs ======
process("Ui/Pages/Models/LaunchSettingsPanelFactory.Controls.cs", [
    ('            Text = "Runtime",\n            Foreground',
     '            Text = Loc.T("Launch.RuntimeLabel"),\n            Foreground'),
    ('            Text = "Port",\n            Foreground',
     '            Text = Loc.T("Launch.PortLabel"),\n            Foreground'),

    # Port tooltip (the one for port LABEL, not the box)
    ('            ToolTip = TooltipText("Fixed server port for this model. OpenCode uses this endpoint before the model is loaded.")',
     '            ToolTip = Loc.T("Tooltip.LaunchPort")'),

    # Runtime combo tooltip
    ('            ToolTip = TooltipText("llama.cpp runtime used when starting or restarting the selected model.")',
     '            ToolTip = Loc.T("Tooltip.RuntimeCombo")'),

    # Search box tooltip
    ('            ToolTip = TooltipText("Filter launch settings by name or description as you type.")',
     '            ToolTip = Loc.T("Tooltip.LaunchSettingsSearch")'),

    # Advanced button tooltip  
    ('            ToolTip = TooltipText("Shows tuning controls for memory, RoPE, speculative/MTP decoding, and sampling.")',
     '            ToolTip = Loc.T("Tooltip.AdvancedSettings")'),

    # AdvancedButtonText method
    ('        => showAdvanced ? "Hide Advanced" : "Advanced Settings";',
     '        => showAdvanced ? Loc.T("Launch.HideAdvanced") : Loc.T("Launch.ShowAdvanced");'),

    # ActionButtons
    ('        saveForModelButton = Button("Save For Model", request.SaveForModelAsync);',
     '        saveForModelButton = Button(Loc.T("Launch.SaveForModelButton"), request.SaveForModelAsync);'),
    ('        actions.Children.Add(Button("Save As Default", request.SaveDefaultsAsync));',
     '        actions.Children.Add(Button(Loc.T("Launch.SaveAsDefaultButton"), request.SaveDefaultsAsync));'),
    ('        actions.Children.Add(Button("Reset Defaults", () =>',
     '        actions.Children.Add(Button(Loc.T("Launch.ResetDefaultsButton"), () =>'),

    # Save as new label + tooltips
    ('            Text = "Save as new",\n            Foreground',
     '            Text = Loc.T("Launch.SaveAsNewLabel"),\n            Foreground'),
    ('            ToolTip = TooltipText("Create a saved model variant from the selected model and the settings currently shown here.")',
     '            ToolTip = Loc.T("Tooltip.SaveAsNewLabel")'),

    # Save as new nameBox tooltip
    ('            ToolTip = TooltipText("Name for the saved model variant. Change the prefilled name before saving.")',
     '            ToolTip = Loc.T("Tooltip.SaveAsNewNameBox")'),

    # Save As New button + its specific tooltip override  
    ('        saveButton = Button("Save As New", request.SaveAsNewAsync);',
     '        saveButton = Button(Loc.T("Launch.SaveAsNewButton"), request.SaveAsNewAsync);'),
    ('        saveButton.ToolTip = TooltipText("Save the current launch settings as a separate loadable model variant on a new direct API port.");',
     '        saveButton.ToolTip = Loc.T("Tooltip.SaveAsNewButton");'),
])

# ====== LaunchSettingsPanelFactory.Sections.cs ======
process("Ui/Pages/Models/LaunchSettingsPanelFactory.Sections.cs", [
    ('builder.AddLaunchSetting(basicGrid, "Context size", contextSizeBox);',
     'builder.AddLaunchSetting(basicGrid, Loc.T("Launch.Field.ContextSize"), contextSizeBox);'),
    ('builder.AddLaunchSetting(basicGrid, "Threads", threadsBox);',
     'builder.AddLaunchSetting(basicGrid, Loc.T("Launch.Field.Threads"), threadsBox);'),
    ('builder.AddLaunchSetting(basicGrid, "GPU layers", gpuLayersBox);',
     'builder.AddLaunchSetting(basicGrid, Loc.T("Launch.Field.GpuLayers"), gpuLayersBox);'),
    ('AddLaunchSection(panel, builder, "Basic Launch", basicGrid);',
     'AddLaunchSection(panel, builder, Loc.T("Launch.Section.Basic"), basicGrid);'),

    ('AddLaunchSection(panel, builder, "Performance & Memory", memoryGrid);',
     'AddLaunchSection(panel, builder, Loc.T("Launch.Section.PerformanceMemory"), memoryGrid);'),

    ('builder.AddLaunchSetting(memoryGrid, "Batch size", batchSizeBox);',
     'builder.AddLaunchSetting(memoryGrid, Loc.T("Launch.Field.BatchSize"), batchSizeBox);'),
    ('builder.AddLaunchSetting(memoryGrid, "Micro batch", microBatchSizeBox);',
     'builder.AddLaunchSetting(memoryGrid, Loc.T("Launch.Field.MicroBatch"), microBatchSizeBox);'),
    ('builder.AddLaunchSetting(memoryGrid, "Flash attention", flashAttentionCombo);',
     'builder.AddLaunchSetting(memoryGrid, Loc.T("Launch.Field.FlashAttention"), flashAttentionCombo);'),
    ('builder.AddLaunchSetting(memoryGrid, "K cache", cacheTypeKCombo);',
     'builder.AddLaunchSetting(memoryGrid, Loc.T("Launch.Field.KCache"), cacheTypeKCombo);'),
    ('builder.AddLaunchSetting(memoryGrid, "V cache", cacheTypeVCombo);',
     'builder.AddLaunchSetting(memoryGrid, Loc.T("Launch.Field.VCache"), cacheTypeVCombo);'),

    # These are in .Memory.cs but let me handle them there instead
    
    ('builder.AddAdvancedLaunchSetting(memoryGrid, "KV offload", kvOffloadCombo);',
     'builder.AddAdvancedLaunchSetting(memoryGrid, Loc.T("Launch.Field.KvOffload"), kvOffloadCombo);'),
    ('builder.AddAdvancedLaunchSetting(memoryGrid, "Unified KV", kvUnifiedCombo);',
     'builder.AddAdvancedLaunchSetting(memoryGrid, Loc.T("Launch.Field.UnifiedKv"), kvUnifiedCombo);'),
    ('builder.AddAdvancedLaunchSetting(memoryGrid, "Prompt cache", promptCacheCombo);',
     'builder.AddAdvancedLaunchSetting(memoryGrid, Loc.T("Launch.Field.PromptCache"), promptCacheCombo);'),
    ('builder.AddAdvancedLaunchSetting(memoryGrid, "Prompt cache MB", promptCacheRamMbBox);',
     'builder.AddAdvancedLaunchSetting(memoryGrid, Loc.T("Launch.Field.PromptCacheMb"), promptCacheRamMbBox);'),
    ('builder.AddAdvancedLaunchSetting(memoryGrid, "Checkpoints", contextCheckpointsCombo);',
     'builder.AddAdvancedLaunchSetting(memoryGrid, Loc.T("Launch.Field.Checkpoints"), contextCheckpointsCombo);'),
    ('builder.AddAdvancedLaunchSetting(memoryGrid, "Checkpoint count", contextCheckpointCountBox);',
     'builder.AddAdvancedLaunchSetting(memoryGrid, Loc.T("Launch.Field.CheckpointCount"), contextCheckpointCountBox);'),
    ('builder.AddAdvancedLaunchSetting(memoryGrid, "Checkpoint spacing", contextCheckpointEveryNTokensBox);',
     'builder.AddAdvancedLaunchSetting(memoryGrid, Loc.T("Launch.Field.CheckpointSpacing"), contextCheckpointEveryNTokensBox);'),
    ('builder.AddAdvancedLaunchSetting(memoryGrid, "Memory map", mmapCombo);',
     'builder.AddAdvancedLaunchSetting(memoryGrid, Loc.T("Launch.Field.MemoryMap"), mmapCombo);'),
    ('builder.AddAdvancedLaunchSetting(memoryGrid, "Memory lock", mlockCombo);',
     'builder.AddAdvancedLaunchSetting(memoryGrid, Loc.T("Launch.Field.MemoryLock"), mlockCombo);'),

    # Speculative/MTP section
    ('AddLaunchSection(panel, builder, "Speculative / MTP", speculativeGrid);',
     'AddLaunchSection(panel, builder, Loc.T("Launch.Section.SpeculativeMtp"), speculativeGrid);'),
    ('builder.AddLaunchSetting(speculativeGrid, "Spec type", speculativeTypeCombo);',
     'builder.AddLaunchSetting(speculativeGrid, Loc.T("Launch.Field.SpecType"), speculativeTypeCombo);'),
    ('builder.AddLaunchSetting(speculativeGrid, "Draft model", specDraftModelPathBox);',
     'builder.AddLaunchSetting(speculativeGrid, Loc.T("Launch.Field.DraftModel"), specDraftModelPathBox);'),
    ('builder.AddLaunchSetting(speculativeGrid, "MTP head", mtpHeadPicker);',
     'builder.AddLaunchSetting(speculativeGrid, Loc.T("Launch.Field.MtpHead"), mtpHeadPicker);'),
    ('builder.AddLaunchSetting(speculativeGrid, "Draft K cache", specDraftCacheTypeKCombo);',
     'builder.AddLaunchSetting(speculativeGrid, Loc.T("Launch.Field.DraftKCache"), specDraftCacheTypeKCombo);'),
    ('builder.AddLaunchSetting(speculativeGrid, "Draft V cache", specDraftCacheTypeVCombo);',
     'builder.AddLaunchSetting(speculativeGrid, Loc.T("Launch.Field.DraftVCache"), specDraftCacheTypeVCombo);'),
    ('builder.AddLaunchSetting(speculativeGrid, "Draft max", specDraftMaxTokensBox);',
     'builder.AddLaunchSetting(speculativeGrid, Loc.T("Launch.Field.DraftMax"), specDraftMaxTokensBox);'),
    ('builder.AddLaunchSetting(speculativeGrid, "Draft min", specDraftMinTokensBox);',
     'builder.AddLaunchSetting(speculativeGrid, Loc.T("Launch.Field.DraftMin"), specDraftMinTokensBox);'),
    ('builder.AddAdvancedLaunchSetting(speculativeGrid, "Draft GPU", specDraftGpuLayersBox);',
     'builder.AddAdvancedLaunchSetting(speculativeGrid, Loc.T("Launch.Field.DraftGpu"), specDraftGpuLayersBox);'),
    ('builder.AddAdvancedLaunchSetting(speculativeGrid, "Split prob", specDraftPSplitBox);',
     'builder.AddAdvancedLaunchSetting(speculativeGrid, Loc.T("Launch.Field.SplitProb"), specDraftPSplitBox);'),
    ('builder.AddAdvancedLaunchSetting(speculativeGrid, "Min prob", specDraftPMinBox);',
     'builder.AddAdvancedLaunchSetting(speculativeGrid, Loc.T("Launch.Field.MinProb"), specDraftPMinBox);'),

    # Chat & Model Capabilities
    ('AddLaunchSection(panel, builder, "Chat & Model Capabilities", chatGrid);',
     'AddLaunchSection(panel, builder, Loc.T("Launch.Section.ChatCapabilities"), chatGrid);'),
    ('builder.AddLaunchSetting(chatGrid, "Reasoning", reasoningCombo);',
     'builder.AddLaunchSetting(chatGrid, Loc.T("Launch.Field.Reasoning"), reasoningCombo);'),
    ('builder.AddLaunchSetting(chatGrid, "Reason format", reasoningFormatCombo);',
     'builder.AddLaunchSetting(chatGrid, Loc.T("Launch.Field.ReasonFormat"), reasoningFormatCombo);'),
    ('builder.AddLaunchSetting(chatGrid, "Reason budget", reasoningBudgetBox);',
     'builder.AddLaunchSetting(chatGrid, Loc.T("Launch.Field.ReasonBudget"), reasoningBudgetBox);'),
    ('builder.AddLaunchSetting(chatGrid, "Jinja chat", jinjaCombo);',
     'builder.AddLaunchSetting(chatGrid, Loc.T("Launch.Field.JinjaChat"), jinjaCombo);'),
    ('builder.AddLaunchSetting(chatGrid, "Vision", visionCombo);',
     'builder.AddLaunchSetting(chatGrid, Loc.T("Launch.Field.Vision"), visionCombo);'),
    ('builder.AddLaunchSetting(chatGrid, "Vision head", visionProjectorPicker);',
     'builder.AddLaunchSetting(chatGrid, Loc.T("Launch.Field.VisionHead"), visionProjectorPicker);'),
    ('builder.AddLaunchSetting(chatGrid, "Image min", visionImageMinTokensBox);',
     'builder.AddLaunchSetting(chatGrid, Loc.T("Launch.Field.ImageMin"), visionImageMinTokensBox);'),
    ('builder.AddLaunchSetting(chatGrid, "Image max", visionImageMaxTokensBox);',
     'builder.AddLaunchSetting(chatGrid, Loc.T("Launch.Field.ImageMax"), visionImageMaxTokensBox);'),

    # Generation Defaults
    ('AddLaunchSection(panel, builder, "Generation Defaults", generationGrid);',
     'AddLaunchSection(panel, builder, Loc.T("Launch.Section.GenerationDefaults"), generationGrid);'),
    ('builder.AddLaunchSetting(generationGrid, "Temperature", temperatureBox);',
     'builder.AddLaunchSetting(generationGrid, Loc.T("Launch.Field.Temperature"), temperatureBox);'),
    ('builder.AddLaunchSetting(generationGrid, "Top K", topKBox);',
     'builder.AddLaunchSetting(generationGrid, Loc.T("Launch.Field.TopK"), topKBox);'),
    ('builder.AddLaunchSetting(generationGrid, "Top P", topPBox);',
     'builder.AddLaunchSetting(generationGrid, Loc.T("Launch.Field.TopP"), topPBox);'),
    ('builder.AddLaunchSetting(generationGrid, "Min P", minPBox);',
     'builder.AddLaunchSetting(generationGrid, Loc.T("Launch.Field.MinP"), minPBox);'),
    ('builder.AddAdvancedLaunchSetting(generationGrid, "Max tokens", maxTokensBox);',
     'builder.AddAdvancedLaunchSetting(generationGrid, Loc.T("Launch.Field.MaxTokens"), maxTokensBox);'),
    ('builder.AddAdvancedLaunchSetting(generationGrid, "Seed", seedBox);',
     'builder.AddAdvancedLaunchSetting(generationGrid, Loc.T("Launch.Field.Seed"), seedBox);'),
    ('builder.AddAdvancedLaunchSetting(generationGrid, "Repeat window", repeatLastNBox);',
     'builder.AddAdvancedLaunchSetting(generationGrid, Loc.T("Launch.Field.RepeatWindow"), repeatLastNBox);'),
    ('builder.AddAdvancedLaunchSetting(generationGrid, "Repeat pen", repeatPenaltyBox);',
     'builder.AddAdvancedLaunchSetting(generationGrid, Loc.T("Launch.Field.RepeatPen"), repeatPenaltyBox);'),
    ('builder.AddAdvancedLaunchSetting(generationGrid, "Presence", presencePenaltyBox);',
     'builder.AddAdvancedLaunchSetting(generationGrid, Loc.T("Launch.Field.Presence"), presencePenaltyBox);'),
    ('builder.AddAdvancedLaunchSetting(generationGrid, "Frequency", frequencyPenaltyBox);',
     'builder.AddAdvancedLaunchSetting(generationGrid, Loc.T("Launch.Field.Frequency"), frequencyPenaltyBox);'),

    # Context Extension  
    ('AddLaunchSection(panel, builder, "Context Extension", ropeGrid, isAdvancedSection: true);',
     'AddLaunchSection(panel, builder, Loc.T("Launch.Section.ContextExtension"), ropeGrid, isAdvancedSection: true);'),
    ('builder.AddLaunchSetting(ropeGrid, "RoPE scaling", ropeScalingCombo);',
     'builder.AddLaunchSetting(ropeGrid, Loc.T("Launch.Field.RopeScaling"), ropeScalingCombo);'),
    ('builder.AddLaunchSetting(ropeGrid, "RoPE scale", ropeScaleBox);',
     'builder.AddLaunchSetting(ropeGrid, Loc.T("Launch.Field.RopeScale"), ropeScaleBox);'),
    ('builder.AddLaunchSetting(ropeGrid, "RoPE base", ropeFreqBaseBox);',
     'builder.AddLaunchSetting(ropeGrid, Loc.T("Launch.Field.RopeBase"), ropeFreqBaseBox);'),
    ('builder.AddLaunchSetting(ropeGrid, "RoPE freq", ropeFreqScaleBox);',
     'builder.AddLaunchSetting(ropeGrid, Loc.T("Launch.Field.RopeFreq"), ropeFreqScaleBox);'),

    # Server section
    ('AddLaunchSection(panel, builder, "Server", serverGrid, isAdvancedSection: true);',
     'AddLaunchSection(panel, builder, Loc.T("Launch.Section.Server"), serverGrid, isAdvancedSection: true);'),
    ('builder.AddLaunchSetting(serverGrid, "Parallel slots", parallelSlotsBox);',
     'builder.AddLaunchSetting(serverGrid, Loc.T("Launch.Field.ParallelSlots"), parallelSlotsBox);'),
    ('builder.AddLaunchSetting(serverGrid, "Continuous batch", continuousBatchingCombo);',
     'builder.AddLaunchSetting(serverGrid, Loc.T("Launch.Field.ContinuousBatch"), continuousBatchingCombo);'),
    ('builder.AddLaunchSetting(serverGrid, "Metrics", metricsCombo);',
     'builder.AddLaunchSetting(serverGrid, Loc.T("Launch.Field.Metrics"), metricsCombo);'),
    ('builder.AddAdvancedLaunchSetting(serverGrid, "Custom params", customParametersBox);',
     'builder.AddAdvancedLaunchSetting(serverGrid, Loc.T("Launch.Field.CustomParams"), customParametersBox);'),
])

# ====== LaunchSettingsPanelFactory.Memory.cs ======
process("Ui/Pages/Models/LaunchSettingsPanelFactory.Memory.cs", [
    ('builder.AddLaunchSetting(memoryGrid, "Batch size", batchSizeBox);',
     'builder.AddLaunchSetting(memoryGrid, Loc.T("Launch.Field.BatchSize"), batchSizeBox);'),
    ('builder.AddLaunchSetting(memoryGrid, "Micro batch", microBatchSizeBox);',
     'builder.AddLaunchSetting(memoryGrid, Loc.T("Launch.Field.MicroBatch"), microBatchSizeBox);'),
    ('builder.AddLaunchSetting(memoryGrid, "Flash attention", flashAttentionCombo);',
     'builder.AddLaunchSetting(memoryGrid, Loc.T("Launch.Field.FlashAttention"), flashAttentionCombo);'),
    ('builder.AddLaunchSetting(memoryGrid, "K cache", cacheTypeKCombo);',
     'builder.AddLaunchSetting(memoryGrid, Loc.T("Launch.Field.KCache"), cacheTypeKCombo);'),
    ('builder.AddLaunchSetting(memoryGrid, "V cache", cacheTypeVCombo);',
     'builder.AddLaunchSetting(memoryGrid, Loc.T("Launch.Field.VCache"), cacheTypeVCombo);'),
])

# ====== LaunchSettingsPanelFactory.Pickers.cs ======
process("Ui/Pages/Models/LaunchSettingsPanelFactory.Pickers.cs", [
    ('        var auto = new MenuItem { Header = "Auto-detect nearby head" };',
     '        var auto = new MenuItem { Header = Loc.T("Picker.Vision.AutoDetectHead") };'),
    ('        var embedded = new MenuItem { Header = "Embedded / model-bundled" };',
     '        var embedded = new MenuItem { Header = Loc.T("Picker.Vision.Embedded") };'),
    # First occurrence of "Choose GGUF file..." in Vision menu
    ('        var choose = new MenuItem { Header = "Choose GGUF file..." };\n        choose.Click += async (_, _) => await chooseAsync();\n\n        menu.Items.Add(auto);\n        menu.Items.Add(embedded);',
     '        var choose = new MenuItem { Header = Loc.T("Picker.ChooseGgufFile") };\n        choose.Click += async (_, _) => await chooseAsync();\n\n        menu.Items.Add(auto);\n        menu.Items.Add(embedded);'),

    # MTP picker - auto detect + choose
    ('        var auto2 = new MenuItem { Header = "Auto-detect nearby MTP head" };',
     '        var auto2 = new MenuItem { Header = Loc.T("Picker.Mtp.AutoDetectHead") };'),
    
    # Second occurrence of "Choose GGUF file..." in MTP menu - need to find unique context  
    ('        var choose2 = new MenuItem { Header = "Choose GGUF file..." };\n        choose2.Click',
     '        var choose2 = new MenuItem { Header = Loc.T("Picker.ChooseGgufFile") };\n        choose2.Click'),

    # MtpHeadButtonText method returns  
    ('        if (string.IsNullOrWhiteSpace(trimmed)) return "Auto-detect MTP head";',
     '        if (string.IsNullOrWhiteSpace(trimmed)) return Loc.T("Picker.Mtp.DefaultText");'),
    ('        return string.IsNullOrWhiteSpace(fileName) ? "MTP head selected" : fileName;',
     '        return string.IsNullOrWhiteSpace(fileName) ? Loc.T("Picker.Mtp.SelectedText") : fileName;'),
])

print("Phase 1 done, continuing...")
