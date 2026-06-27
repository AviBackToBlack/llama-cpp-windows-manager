using System.Windows.Controls;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace LocalLlmConsole;

public static partial class LaunchSettingsPanelFactory
{
    private static MemoryLaunchControls AddPerformanceMemorySettings(
        Grid memoryGrid,
        LaunchSettingsPanelBuilder builder,
        AppSettings settings)
    {
        var batchSizeBox = LaunchTextBox(settings.BatchSize);
        builder.AddLaunchSetting(memoryGrid, Loc.T("Launch.Field.BatchSize"), batchSizeBox);
        var microBatchSizeBox = LaunchTextBox(settings.MicroBatchSize);
        builder.AddLaunchSetting(memoryGrid, Loc.T("Launch.Field.MicroBatch"), microBatchSizeBox);
        var flashAttentionCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        builder.AddLaunchSetting(memoryGrid, Loc.T("Launch.Field.FlashAttention"), flashAttentionCombo);
        var cacheTypeKCombo = LaunchCombo(LaunchSettingMetadataService.CacheTypeOptions);
        builder.AddLaunchSetting(memoryGrid, Loc.T("Launch.Field.KCache"), cacheTypeKCombo);
        var cacheTypeVCombo = LaunchCombo(LaunchSettingMetadataService.CacheTypeOptions);
        builder.AddLaunchSetting(memoryGrid, Loc.T("Launch.Field.VCache"), cacheTypeVCombo);
        var kvOffloadCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        builder.AddAdvancedLaunchSetting(memoryGrid, Loc.T("Launch.Field.KvOffload"), kvOffloadCombo);
        var kvUnifiedCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        builder.AddAdvancedLaunchSetting(memoryGrid, Loc.T("Launch.Field.UnifiedKv"), kvUnifiedCombo);
        var promptCacheCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        builder.AddAdvancedLaunchSetting(memoryGrid, Loc.T("Launch.Field.PromptCache"), promptCacheCombo);
        var promptCacheRamMbBox = LaunchTextBox(settings.PromptCacheRamMb);
        builder.AddAdvancedLaunchSetting(memoryGrid, Loc.T("Launch.Field.PromptCacheMb"), promptCacheRamMbBox);
        var contextCheckpointsCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        builder.AddAdvancedLaunchSetting(memoryGrid, Loc.T("Launch.Field.Checkpoints"), contextCheckpointsCombo);
        var contextCheckpointCountBox = LaunchTextBox(settings.ContextCheckpointCount);
        builder.AddAdvancedLaunchSetting(memoryGrid, Loc.T("Launch.Field.CheckpointCount"), contextCheckpointCountBox);
        var contextCheckpointEveryNTokensBox = LaunchTextBox(settings.ContextCheckpointEveryNTokens);
        builder.AddAdvancedLaunchSetting(memoryGrid, Loc.T("Launch.Field.CheckpointSpacing"), contextCheckpointEveryNTokensBox);
        var mmapCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        builder.AddAdvancedLaunchSetting(memoryGrid, Loc.T("Launch.Field.MemoryMap"), mmapCombo);
        var mlockCombo = LaunchCombo(LaunchSettingMetadataService.OffOnOptions);
        builder.AddAdvancedLaunchSetting(memoryGrid, Loc.T("Launch.Field.MemoryLock"), mlockCombo);

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
