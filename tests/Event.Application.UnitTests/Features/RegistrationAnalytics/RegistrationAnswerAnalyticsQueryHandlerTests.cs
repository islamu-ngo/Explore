// ABOUTME: Unit tests for mapping governed answer analytics projections into API DTOs.
// ABOUTME: Ensures tenant-scoped repository calls stay inside Application before DTO projection.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Authorization;
using Explore.Application.Features.RegistrationAnalytics;
using Explore.Domain;
using NSubstitute;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.RegistrationAnalytics;

public sealed class RegistrationAnswerAnalyticsQueryHandlerTests
{
    [Test]
    public async Task Handle_UsesTenantScopedProjectionAndMapsDtoShape()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid formId = Guid.CreateVersion7();
        Guid versionId = Guid.CreateVersion7();
        var repository = Substitute.For<IRegistrationAnswerAnalyticsRepository>();
        repository.GetEventFormVersionAnalyticsAsync(tenantId, eventId, formId, versionId, 3, Arg.Any<CancellationToken>())
            .Returns(new RegistrationAnswerAnalyticsProjection(eventId, formId, versionId, 3,
            [
                new RegistrationAnswerFieldAggregateProjection(
                    Guid.CreateVersion7(), "person", "age", "Age", 3, "INTEGER", true, 3, [],
                    new RegistrationAnswerNumericAggregateProjection(3, 18, 44, 30))
            ]));
        var handler = new GetRegistrationAnswerAnalyticsQueryHandler(repository, new Tenant(tenantId));

        var dto = await handler.Handle(new GetRegistrationAnswerAnalyticsQuery(eventId, formId, versionId), CancellationToken.None);

        await Assert.That(dto).IsNotNull();
        await Assert.That(dto!.MinimumCellSize).IsEqualTo(3);
        await Assert.That(dto.Fields.Single().Numeric!.Average).IsEqualTo(30);
        await repository.Received(1).GetEventFormVersionAnalyticsAsync(tenantId, eventId, formId, versionId, 3, Arg.Any<CancellationToken>());

        // The handler is the trusted resolver for the tenant: the projection does not carry one, so if the
        // handler stops publishing it the HAL self link loses its event context and is denied for every caller.
        await Assert.That(dto.TenantId).IsEqualTo(tenantId);
    }

    [Test]
    public async Task Query_CarriesManageRegistrationsAuthorizationContext()
    {
        Guid eventId = Guid.CreateVersion7();
        Guid formId = Guid.CreateVersion7();
        Guid versionId = Guid.CreateVersion7();
        var query = new GetRegistrationAnswerAnalyticsQuery(eventId, formId, versionId);
        var secure = (ISecureRequest)query;

        await Assert.That(typeof(GetRegistrationAnswerAnalyticsQuery)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: false)
            .Cast<AuthorizeResourceAttribute>()
            .Single().Action).IsEqualTo(AuthorizationActions.Events.ManageRegistrations);
        await Assert.That(secure.ResourceId).IsEqualTo(eventId.ToString("D"));
        // The form and version identify which answers are aggregated; the parent event decides access.
        await Assert.That(secure.AuthorizationFacts).IsTypeOf<EventScopedAuthorizationFacts>();
    }

    private sealed record Tenant(Guid TenantId) : ITenantContext;
}
