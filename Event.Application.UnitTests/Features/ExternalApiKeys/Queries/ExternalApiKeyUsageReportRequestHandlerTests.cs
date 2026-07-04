// ABOUTME: Unit tests for external API-key usage report authorization semantics.
// ABOUTME: Verifies tenant-scoped and platform-wide reports fail closed before repository reads.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.ExternalApiKeys.Handlers.Queries;
using Explore.Application.Features.ExternalApiKeys.Requests.Queries;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.ExternalApiKeys.Queries;

public class ExternalApiKeyUsageReportRequestHandlerTests
{
    [Test]
    public async Task Handle_WithTenantIdAndNoTenantOrInstanceAdmin_ThrowsBeforeRepositoryRead()
    {
        var tenantId = Guid.NewGuid();
        var quotaRepository = Substitute.For<IExternalApiKeyQuotaRepository>();
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.IsTenantAdminAsync(tenantId, Arg.Any<CancellationToken>()).Returns(false);
        adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetExternalApiKeyUsageReportRequestHandler(quotaRepository, adminContext);

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            handler.Handle(CreateRequest(tenantId), CancellationToken.None));

        await quotaRepository.DidNotReceive().GetUsageByTenant(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
        await quotaRepository.DidNotReceive().GetUsagePlatformWide(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithTenantIdAndTenantAdmin_ReadsOnlyRequestedTenant()
    {
        var tenantId = Guid.NewGuid();
        var apiKeyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var quotaRepository = Substitute.For<IExternalApiKeyQuotaRepository>();
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.IsTenantAdminAsync(tenantId, Arg.Any<CancellationToken>()).Returns(true);
        quotaRepository.GetUsageByTenant(tenantId, DateOnly.Parse("2026-01-01"), DateOnly.Parse("2026-01-31"), Arg.Any<CancellationToken>())
            .Returns(
            [
                new TenantApiKeyUsageSummary(
                    apiKeyId,
                    "Tenant Bot",
                    tenantId,
                    (int)ExternalApiKeyOwnerType.Tenant,
                    ownerId,
                    TotalRequestCount: 42,
                    TotalCreditsUsed: 7,
                    CreditLimit: 100)
            ]);
        var handler = new GetExternalApiKeyUsageReportRequestHandler(quotaRepository, adminContext);

        var result = await handler.Handle(CreateRequest(tenantId), CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].ApiKeyId).IsEqualTo(apiKeyId);
        await Assert.That(result[0].TenantId).IsEqualTo(tenantId);
        await quotaRepository.Received(1).GetUsageByTenant(tenantId, DateOnly.Parse("2026-01-01"), DateOnly.Parse("2026-01-31"), Arg.Any<CancellationToken>());
        await quotaRepository.DidNotReceive().GetUsagePlatformWide(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithoutTenantIdAndNoInstanceAdmin_ThrowsBeforeRepositoryRead()
    {
        var quotaRepository = Substitute.For<IExternalApiKeyQuotaRepository>();
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetExternalApiKeyUsageReportRequestHandler(quotaRepository, adminContext);

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            handler.Handle(CreateRequest(tenantId: null), CancellationToken.None));

        await quotaRepository.DidNotReceive().GetUsageByTenant(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
        await quotaRepository.DidNotReceive().GetUsagePlatformWide(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithoutTenantIdAndInstanceAdmin_ReadsPlatformWide()
    {
        var quotaRepository = Substitute.For<IExternalApiKeyQuotaRepository>();
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        quotaRepository.GetUsagePlatformWide(DateOnly.Parse("2026-01-01"), DateOnly.Parse("2026-01-31"), Arg.Any<CancellationToken>())
            .Returns([]);
        var handler = new GetExternalApiKeyUsageReportRequestHandler(quotaRepository, adminContext);

        var result = await handler.Handle(CreateRequest(tenantId: null), CancellationToken.None);

        await Assert.That(result).IsEmpty();
        await quotaRepository.Received(1).GetUsagePlatformWide(DateOnly.Parse("2026-01-01"), DateOnly.Parse("2026-01-31"), Arg.Any<CancellationToken>());
        await quotaRepository.DidNotReceive().GetUsageByTenant(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    private static GetExternalApiKeyUsageReportRequest CreateRequest(Guid? tenantId) =>
        new()
        {
            From = DateOnly.Parse("2026-01-01"),
            To = DateOnly.Parse("2026-01-31"),
            TenantId = tenantId
        };
}
