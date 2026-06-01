// ABOUTME: Unit-style API feature tests for tenant storage settings command behavior.
// ABOUTME: Verifies lock, upload ceiling, and cross-tenant authority failures before persistence.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Features.TenantStorageSettings.Handlers.Commands;
using Explore.Application.Features.TenantStorageSettings.Requests.Commands;
using Explore.Domain;
using Event.Api.IntegrationTests.Fixtures;
using NSubstitute;

namespace Event.Api.IntegrationTests.Features;

[Category(TestCategories.Fast)]
[Category("TenantStorageSettings")]
public sealed class TenantStorageSettingsCommandHandlerTests
{
    private static readonly Guid TenantA = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    private static readonly Guid TenantB = Guid.Parse("018e4e5c-7f00-7000-8000-000000000099");
    private static readonly Guid UserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000123");

    [Test]
    public async Task Handle_WhenTenantStorageIsLocked_ReturnsFailureWithoutApplyingSettings()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TenantA);
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.IsTenantAdminAsync(TenantA, Arg.Any<CancellationToken>()).Returns(true);
        var storageService = Substitute.For<ITenantStorageSettingService>();
        storageService.ReadSettingsAsync(TenantA, Arg.Any<CancellationToken>())
            .Returns(CreateCurrentSettings(isReadOnly: true));
        var handler = CreateHandler(tenantContext, adminContext, storageService);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("StorageTenantOverridesLocked");
        await storageService.DidNotReceive().ApplySettingsAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<TenantStorageSettingsDto>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenMaxUploadExceedsInstanceCeiling_ReturnsValidationFailure()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TenantA);
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.IsTenantAdminAsync(TenantA, Arg.Any<CancellationToken>()).Returns(true);
        var storageService = Substitute.For<ITenantStorageSettingService>();
        storageService.ReadSettingsAsync(TenantA, Arg.Any<CancellationToken>())
            .Returns(CreateCurrentSettings(isReadOnly: false, instanceMaxUploadBytes: 100));
        var handler = CreateHandler(tenantContext, adminContext, storageService);

        var result = await handler.Handle(CreateCommand(maxUploadBytes: 101), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Tenant storage settings validation failed.");
        await Assert.That(result.Errors!.Any(error => error.Contains("instance ceiling", StringComparison.OrdinalIgnoreCase))).IsTrue();
        await storageService.DidNotReceive().ApplySettingsAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<TenantStorageSettingsDto>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenTenantAdminTargetsDifferentTenant_ReturnsForbiddenFailure()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TenantB);
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.IsTenantAdminAsync(TenantB, Arg.Any<CancellationToken>()).Returns(false);
        adminContext.GetAdminTenantIdsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { TenantA });
        adminContext.IsInstanceAdminAsync(UserId, Arg.Any<CancellationToken>()).Returns(false);
        var storageService = Substitute.For<ITenantStorageSettingService>();
        var handler = CreateHandler(tenantContext, adminContext, storageService);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("Only tenant administrators");
        await storageService.DidNotReceive().ReadSettingsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static UpdateTenantStorageSettingsCommandHandler CreateHandler(
        ITenantContext tenantContext,
        IAdminContext adminContext,
        ITenantStorageSettingService storageService)
        => new(
            tenantContext,
            adminContext,
            storageService,
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IHierarchicalSettingsResolver>(),
            Substitute.For<IS3ConfigResolver>());

    private static UpdateTenantStorageSettingsCommand CreateCommand(long maxUploadBytes = 64)
        => new()
        {
            UserId = UserId,
            Settings = new TenantStorageSettingsDto
            {
                Provider = StorageProviders.Local,
                MaxUploadBytes = maxUploadBytes,
                TenantQuotaBytes = 1024,
                S3UploadUrlExpirationMinutes = 60
            }
        };

    private static TenantStorageSettingsDto CreateCurrentSettings(
        bool isReadOnly,
        long instanceMaxUploadBytes = 128)
        => new()
        {
            TenantId = TenantA,
            Provider = StorageProviders.Local,
            MaxUploadBytes = 64,
            TenantQuotaBytes = 1024,
            IsReadOnly = isReadOnly,
            TenantOverridesAllowed = !isReadOnly,
            EffectivePolicy = new TenantStorageEffectivePolicyDto
            {
                Provider = StorageProviders.Local,
                MaxUploadBytes = 64,
                TenantQuotaBytes = 1024,
                InstanceMaxUploadBytes = instanceMaxUploadBytes,
                TenantOverridesAllowed = !isReadOnly
            }
        };
}
