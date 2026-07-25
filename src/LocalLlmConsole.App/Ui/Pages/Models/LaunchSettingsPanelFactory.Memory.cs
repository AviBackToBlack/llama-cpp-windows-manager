using System.Windows.Controls;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace LocalLlmConsole;

public static partial class LaunchSettingsPanelFactory
{
    private static MemoryLaunchControls AddPerformanceMemorySettings(
        Grid memoryGrid,
        LaunchSettingsPanelBuilder builder,
        AppSettings settings,
        HashSet<string> excludedFlags)
    {
        var batchSizeBox = LaunchTextBox(settings.BatchSize);
        AddFirstClassControl(memoryGrid, builder, Loc.T("Launch.Field.BatchSize"), "--batch-size", batchSizeBox, excludedFlags);
        var microBatchSizeBox = LaunchTextBox(settings.MicroBatchSize);
        AddFirstClassControl(memoryGrid, builder, Loc.T("Launch.Field.MicroBatch"), "--ubatch-size", microBatchSizeBox, excludedFlags);
        var flashAttentionCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        SetComboValue(flashAttentionCombo, settings.FlashAttention);
        AddFirstClassControl(memoryGrid, builder, Loc.T("Launch.Field.FlashAttention"), "--flash-attn", flashAttentionCombo, excludedFlags);
        var cacheTypeKCombo = LaunchCombo(LaunchSettingMetadataService.CacheTypeOptions);
        SetComboValue(cacheTypeKCombo, settings.CacheTypeK);
        AddFirstClassControl(memoryGrid, builder, Loc.T("Launch.Field.KCache"), "--cache-type-k", cacheTypeKCombo, excludedFlags);
        var cacheTypeVCombo = LaunchCombo(LaunchSettingMetadataService.CacheTypeOptions);
        SetComboValue(cacheTypeVCombo, settings.CacheTypeV);
        AddFirstClassControl(memoryGrid, builder, Loc.T("Launch.Field.VCache"), "--cache-type-v", cacheTypeVCombo, excludedFlags);
        var kvOffloadCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        SetComboValue(kvOffloadCombo, settings.KvOffload);
        AddFirstClassControl(memoryGrid, builder, Loc.T("Launch.Field.KvOffload"), "--kv-offload", kvOffloadCombo, excludedFlags, advanced: true);
        var kvUnifiedCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        SetComboValue(kvUnifiedCombo, settings.KvUnified);
        AddFirstClassControl(memoryGrid, builder, Loc.T("Launch.Field.UnifiedKv"), "--kv-unified", kvUnifiedCombo, excludedFlags, advanced: true);
        var promptCacheCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        SetComboValue(promptCacheCombo, settings.PromptCacheMode);
        AddFirstClassControl(memoryGrid, builder, Loc.T("Launch.Field.PromptCache"), "--cache-ram-mode", promptCacheCombo, excludedFlags, advanced: true);
        var promptCacheRamMbBox = LaunchTextBox(settings.PromptCacheRamMb);
        AddFirstClassControl(memoryGrid, builder, Loc.T("Launch.Field.PromptCacheMb"), "--cache-ram", promptCacheRamMbBox, excludedFlags, advanced: true);
        var contextCheckpointsCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        SetComboValue(contextCheckpointsCombo, settings.ContextCheckpointsMode);
        AddFirstClassControl(memoryGrid, builder, Loc.T("Launch.Field.Checkpoints"), "--ctx-checkpoints-mode", contextCheckpointsCombo, excludedFlags, advanced: true);
        var contextCheckpointCountBox = LaunchTextBox(settings.ContextCheckpointCount);
        AddFirstClassControl(memoryGrid, builder, Loc.T("Launch.Field.CheckpointCount"), "--ctx-checkpoints", contextCheckpointCountBox, excludedFlags, advanced: true);
        var contextCheckpointEveryNTokensBox = LaunchTextBox(settings.ContextCheckpointEveryNTokens);
        AddFirstClassControl(memoryGrid, builder, Loc.T("Launch.Field.CheckpointSpacing"), "--checkpoint-min-step", contextCheckpointEveryNTokensBox, excludedFlags, advanced: true);
        var mmapCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        SetComboValue(mmapCombo, settings.MmapMode);
        AddFirstClassControl(memoryGrid, builder, Loc.T("Launch.Field.MemoryMap"), "--mmap", mmapCombo, excludedFlags, advanced: true);
        var mlockCombo = LaunchCombo(LaunchSettingMetadataService.OffOnOptions);
        SetComboValue(mlockCombo, settings.MlockMode);
        AddFirstClassControl(memoryGrid, builder, Loc.T("Launch.Field.MemoryLock"), "--mlock", mlockCombo, excludedFlags, advanced: true);

        return new MemoryLaunchControls(
            batchSizeBox,
            microBatchSizeBox,
            flashAttentionCombo,
            cacheTypeKCombo,
            cacheTypeVCombo,
            kvOffloadCombo,
            kvUnifiedCombo,
            promptCacheCombo,
            promptCacheRamMbBox,
            contextCheckpointsCombo,
            contextCheckpointCountBox,
            contextCheckpointEveryNTokensBox,
            mmapCombo,
            mlockCombo);
    }

    private sealed record MemoryLaunchControls(
        WpfTextBox BatchSizeBox,
        WpfTextBox MicroBatchSizeBox,
        WpfComboBox FlashAttentionCombo,
        WpfComboBox CacheTypeKCombo,
        WpfComboBox CacheTypeVCombo,
        WpfComboBox KvOffloadCombo,
        WpfComboBox KvUnifiedCombo,
        WpfComboBox PromptCacheCombo,
        WpfTextBox PromptCacheRamMbBox,
        WpfComboBox ContextCheckpointsCombo,
        WpfTextBox ContextCheckpointCountBox,
        WpfTextBox ContextCheckpointEveryNTokensBox,
        WpfComboBox MmapCombo,
        WpfComboBox MlockCombo);
}
