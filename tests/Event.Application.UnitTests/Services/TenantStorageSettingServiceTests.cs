// ABOUTME: Focused tests for presence-aware tenant storage override persistence.
// ABOUTME: Proves each PATCH leaf writes only its own setting key and preserves omitted siblings.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Models.Common;
using Explore.Application.Models.Storage;
using Explore.Application.Services;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public sealed class TenantStorageSettingServiceTests
{
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly Guid ActorId = Guid.CreateVersion7();

    private readonly IHierarchicalSettingsResolver _settingsResolver =
        Substitute.For<IHierarchicalSettingsResolver>();
    private readonly ITenantSettingRepository _tenantSettingRepository =
        Substitute.For<ITenantSettingRepository>();
    private readonly IStoragePolicyResolver _storagePolicyResolver =
        Substitute.For<IStoragePolicyResolver>();

    [Test]
    public async Task ApplyPatchAsync_WhenOnlyPolicyLeafIsPresent_WritesOnlyThatPolicyKey()
    {
        var writes = CaptureWrites();
        var service = CreateService();
        var patch = new PatchTenantStorageSettingsDto
        {
            Policy = new PatchTenantStoragePolicyDto
            {
                MaxUploadBytes = OptionalUpdate<long>.Set(4096)
            }
        };

        await service.ApplyPatchAsync(TenantId, ActorId, patch);

        await Assert.That(writes).IsEquivalentTo([
            (GovernanceSettingKeys.Storage.DefaultMaxUploadBytes, "4096")
        ]);
        _settingsResolver.DidNotReceive().InvalidateCache(Arg.Any<SettingScope?>(), Arg.Any<Guid?>());
    }

    [Test]
    public async Task ApplyPatchAsync_WhenOnlyS3LeafIsPresent_WritesOnlyThatS3Key()
    {
        var writes = CaptureWrites();
        var service = CreateService();
        var patch = new PatchTenantStorageSettingsDto
        {
            S3 = new PatchTenantStorageS3Dto
            {
                Endpoint = OptionalUpdate<string>.Set("  https://s3.example.test  ")
            }
        };

        await service.ApplyPatchAsync(TenantId, ActorId, patch);

        await Assert.That(writes).IsEquivalentTo([
            (GovernanceSettingKeys.Storage.Endpoint, "\"https://s3.example.test\"")
        ]);
        _settingsResolver.DidNotReceive().InvalidateCache(Arg.Any<SettingScope?>(), Arg.Any<Guid?>());
    }

    [Test]
    public async Task TestProviderAsync_RequestsWriteProbeAndMapsPreflight()
    {
        var preflight = new S3PreflightResult { IsSuccess = true, CanRead = true, CanWrite = true };
        var provider = Substitute.For<IFileStorageProvider>();
        provider.TestAsync(Arg.Any<CancellationToken>(), true)
            .Returns(new FileStorageProviderStatus(
                StorageProviders.S3Compatible,
                true,
                false,
                true,
                Preflight: preflight));
        _storagePolicyResolver.ResolveProviderAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(provider);

        var result = await CreateService().TestProviderAsync(TenantId);

        await Assert.That(result.Preflight).IsSameReferenceAs(preflight);
        await Assert.That(result.IsAvailable).IsTrue();
        await provider.Received(1).TestAsync(Arg.Any<CancellationToken>(), true);
    }

    private List<(string Key, string Value)> CaptureWrites()
    {
        var writes = new List<(string Key, string Value)>();
        _tenantSettingRepository.SetValueAsync(
                TenantId,
                Arg.Do<string>(key => writes.Add((key, string.Empty))),
                Arg.Do<string>(value => writes[^1] = (writes[^1].Key, value)),
                Arg.Any<CancellationToken>(),
                ActorId)
            .Returns(Task.CompletedTask);
        return writes;
    }

    private TenantStorageSettingService CreateService()
        => new(
            _settingsResolver,
            _tenantSettingRepository,
            _storagePolicyResolver,
            Substitute.For<IStorageUsageCounterRepository>());
}
