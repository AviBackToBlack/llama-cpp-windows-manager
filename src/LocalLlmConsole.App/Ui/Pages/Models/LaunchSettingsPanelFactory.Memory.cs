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
        builder.AddLaunchSetting(memoryGrid, "Batch size", batchSizeBox);
        var microBatchSizeBox = LaunchTextBox(settings.MicroBatchSize);
        builder.AddLaunchSetting(memoryGrid, "Micro batch", microBatchSizeBox);
        var flashAttentionCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        builder.AddLaunchSetting(memoryGrid, "Flash attention", flashAttentionCombo);
        var cacheTypeKCombo = LaunchCombo(LaunchSettingMetadataService.CacheTypeOptions);
        builder.AddLaunchSetting(memoryGrid, "K cache", cacheTypeKCombo);
        var cacheTypeVCombo = LaunchCombo(LaunchSettingMetadataService.CacheTypeOptions);
        builder.AddLaunchSetting(memoryGrid, "V cache", cacheTypeVCombo);
        var kvOffloadCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        builder.AddAdvancedLaunchSetting(memoryGrid, "KV offload", kvOffloadCombo);
        var kvUnifiedCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        builder.AddAdvancedLaunchSetting(memoryGrid, "Unified KV", kvUnifiedCombo);
        var promptCacheCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        builder.AddAdvancedLaunchSetting(memoryGrid, "Prompt cache", promptCacheCombo);
        var promptCacheRamMbBox = LaunchTextBox(settings.PromptCacheRamMb);
        builder.AddAdvancedLaunchSetting(memoryGrid, "Prompt cache MB", promptCacheRamMbBox);
        var contextCheckpointsCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        builder.AddAdvancedLaunchSetting(memoryGrid, "Checkpoints", contextCheckpointsCombo);
        var contextCheckpointCountBox = LaunchTextBox(settings.ContextCheckpointCount);
        builder.AddAdvancedLaunchSetting(memoryGrid, "Checkpoint count", contextCheckpointCountBox);
        var contextCheckpointEveryNTokensBox = LaunchTextBox(settings.ContextCheckpointEveryNTokens);
        builder.AddAdvancedLaunchSetting(memoryGrid, "Checkpoint spacing", contextCheckpointEveryNTokensBox);
        var mmapCombo = LaunchCombo(LaunchSettingMetadataService.AutoOnOffOptions);
        builder.AddAdvancedLaunchSetting(memoryGrid, "Memory map", mmapCombo);
        var mlockCombo = LaunchCombo(LaunchSettingMetadataService.OffOnOptions);
        builder.AddAdvancedLaunchSetting(memoryGrid, "Memory lock", mlockCombo);

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
