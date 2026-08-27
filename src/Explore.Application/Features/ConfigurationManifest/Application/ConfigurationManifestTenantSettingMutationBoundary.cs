// ABOUTME: Owns typed tenant-setting writes performed inside a ConfigurationManifest transaction.
// ABOUTME: Rechecks catalog ownership and blocks guarded keys before reaching persistence.

namespace Explore.Application.Features.ConfigurationManifest.Application;

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ConfigurationManifest.Catalog;

public sealed record ConfigurationManifestTenantSettingMutation(
    string Key,
    string SerializedValue);

public sealed record ConfigurationManifestTenantSettingMutationInput(
    Guid TenantId,
    IReadOnlyList<ConfigurationManifestTenantSettingMutation> Mutations,
    Guid? ActorUserId,
    DateTime OccurredAtUtc);

public interface IConfigurationManifestTenantSettingMutationBoundary
{
    Task CreateInCurrentTransactionAsync(
        ConfigurationManifestTenantSettingMutationInput input,
        CancellationToken cancellationToken = default);
}

public sealed class ConfigurationManifestTenantSettingMutationBoundary(
    ITenantSettingRepository tenantSettings)
    : IConfigurationManifestTenantSettingMutationBoundary
{
    public async Task CreateInCurrentTransactionAsync(
        ConfigurationManifestTenantSettingMutationInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Mutations);
        ArgumentOutOfRangeException.ThrowIfEqual(input.TenantId, Guid.Empty);
        if (input.OccurredAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Tenant setting mutation timestamp must use UTC kind.",
                nameof(input));
        }

        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        var writes = new List<TenantSettingOverrideUpsert>(input.Mutations.Count);
        foreach (ConfigurationManifestTenantSettingMutation mutation
                 in input.Mutations)
        {
            ArgumentNullException.ThrowIfNull(mutation);
            if (!seenKeys.Add(mutation.Key)
                || !ConfigurationManifestCatalog.TryGetTenantSetting(
                    mutation.Key,
                    out ConfigurationManifestSettingCatalogEntry? catalogEntry)
                || catalogEntry is null
                || catalogEntry.Definition.RequiresCoordinatedMutation)
            {
                throw new InvalidOperationException(
                    "The tenant setting mutation is not owned by this boundary.");
            }

            writes.Add(new TenantSettingOverrideUpsert(
                mutation.Key,
                mutation.SerializedValue,
                IsLocked: false));
        }

        if (writes.Count == 0)
            return;

        await tenantSettings.CreateManyForTenantAsync(
            input.TenantId,
            writes,
            input.ActorUserId,
            input.OccurredAtUtc,
            cancellationToken);
    }
}
