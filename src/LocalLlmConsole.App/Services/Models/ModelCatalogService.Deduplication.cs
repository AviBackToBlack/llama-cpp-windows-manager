namespace LocalLlmConsole.Services;

public sealed partial class ModelCatalogService
{
    public async Task<int> CleanupModelRecordsAsync()
    {
        var changed = await CleanupDuplicateModelRecordsAsync();
        return changed + await NormalizeFriendlyModelNamesAsync();
    }

    public async Task<int> CleanupDuplicateModelRecordsAsync()
    {
        var removed = 0;
        var duplicateGroups = (await _store.ListModelsAsync())
            .Where(model => model.Ownership != OwnershipKind.RegistryOnly)
            .GroupBy(model => NormalizePath(model.ModelPath), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .ToArray();

        foreach (var group in duplicateGroups)
        {
            var canonical = group
                .OrderBy(model => model.Ownership switch
                {
                    OwnershipKind.AppOwned => 0,
                    OwnershipKind.External => 1,
                    _ => 2
                })
                .ThenByDescending(model => model.UpdatedAt)
                .First();
            removed += group.Count() - 1;
            await RemoveDuplicateModelRecordsForPathAsync(canonical);
        }

        return removed;
    }

    private async Task<int> NormalizeFriendlyModelNamesAsync()
    {
        var updated = 0;
        foreach (var model in await _store.ListModelsAsync())
        {
            if (ModelAliasService.IsLaunchAlias(model)) continue;
            if (!model.Name.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase)) continue;

            var friendlyName = FriendlyDisplayName(model.Name, model.ModelPath);
            if (string.Equals(model.Name, friendlyName, StringComparison.Ordinal)) continue;

            await _store.UpsertModelAsync(model with { Name = friendlyName, UpdatedAt = DateTimeOffset.UtcNow });
            updated++;
        }

        return updated;
    }

    private async Task RemoveDuplicateModelRecordsForPathAsync(ModelRecord canonical)
    {
        var canonicalPath = NormalizePath(canonical.ModelPath);
        var duplicates = (await _store.ListModelsAsync())
            .Where(model => !string.Equals(model.Id, canonical.Id, StringComparison.OrdinalIgnoreCase)
                && model.Ownership != OwnershipKind.RegistryOnly
                && string.Equals(NormalizePath(model.ModelPath), canonicalPath, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (duplicates.Length == 0) return;

        var canonicalProfiles = (await _store.ListNamedModelLaunchProfilesAsync(canonical.Id)).ToList();
        foreach (var duplicate in duplicates)
        {
            foreach (var profile in await _store.ListNamedModelLaunchProfilesAsync(duplicate.Id))
            {
                if (profile.IsDefault && canonicalProfiles.Any(candidate => candidate.IsDefault)) continue;
                if (canonicalProfiles.Any(candidate => string.Equals(candidate.Name, profile.Name, StringComparison.OrdinalIgnoreCase))) continue;

                var moved = profile with { ModelId = canonical.Id, UpdatedAt = DateTimeOffset.UtcNow };
                await _store.SaveNamedModelLaunchProfileAsync(moved);
                canonicalProfiles.Add(moved);
            }
        }

        foreach (var duplicate in duplicates)
            await _store.DeleteModelAsync(duplicate.Id);
    }
}
