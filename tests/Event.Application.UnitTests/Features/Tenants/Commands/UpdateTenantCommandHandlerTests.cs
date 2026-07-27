// ABOUTME: Unit tests for grouped Tenant metadata updates and slug-cache convergence.
// ABOUTME: Verifies a persisted slug change refreshes authoritative tenant routing state.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Features.Tenants.Handlers.Commands;
using Explore.Application.Features.Tenants.Requests.Commands;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Tenants.Commands;

public sealed class UpdateTenantCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenSlugChanges_RefreshesTenantSlugCacheAfterPersistence()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            FullName = "Community",
            Slug = "old-slug",
            TenantStatus = null!
        };
        var repository = Substitute.For<ITenantRepository>();
        var slugCache = Substitute.For<ITenantSlugCache>();
        repository.GetById(tenantId).Returns(tenant);
        var handler = new UpdateTenantCommandHandler(repository, slugCache);

        var result = await handler.Handle(
            new UpdateTenantCommand
            {
                TenantId = tenantId,
                Update = new UpdateTenantDto { Slug = new() { Value = "new-slug" } }
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await repository.Received(1).Update(Arg.Is<Tenant>(value => value.Slug == "new-slug"));
        await slugCache.Received(1).RefreshAsync(Arg.Any<CancellationToken>());
    }
}
