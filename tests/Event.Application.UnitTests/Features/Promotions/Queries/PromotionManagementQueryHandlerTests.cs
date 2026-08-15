// ABOUTME: Covers organizer promotion management query mapping with in-memory Application-layer fakes.
// ABOUTME: Verifies safe DTO serialization hides authority metadata and promotion lookup secrets.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Promotions;
using Explore.Application.Features.Promotions.Handlers.Queries;
using Explore.Application.Features.Promotions.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace ApplicationUnitTests.Features.Promotions.Queries;

public sealed class PromotionManagementQueryHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IPromotionManagementRepository _promotions = Substitute.For<IPromotionManagementRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();

    public PromotionManagementQueryHandlerTests()
    {
        _tenant.TenantId.Returns(_tenantId);
    }

    [Test]
    public async Task ListAndGetAsyncReturnSafeManagementDtosWithHiddenAuthorityMetadata()
    {
        PromotionManagementScenario scenario = CreateScenario();
        _events.GetAuthorizationTargetByIdAsync(scenario.Event.Id, Arg.Any<CancellationToken>()).Returns(scenario.Event);
        _promotions.ListManagementEntriesAsync(_tenantId, scenario.Event.Id, scenario.Catalog.Id, Arg.Any<CancellationToken>()).Returns([scenario.Entry]);
        _promotions.GetManagementEntryAsync(_tenantId, scenario.Event.Id, scenario.Definition.Id, Arg.Any<CancellationToken>()).Returns(scenario.Entry);

        IReadOnlyList<PromotionManagementDto> list = await new ListPromotionManagementQueryHandler(_events, _promotions, _tenant)
            .Handle(new ListPromotionManagementQuery(scenario.Event.Id, scenario.Catalog.Id), CancellationToken.None);
        PromotionManagementDto? detail = await new GetPromotionManagementQueryHandler(_events, _promotions, _tenant)
            .Handle(new GetPromotionManagementQuery(scenario.Event.Id, scenario.Definition.Id), CancellationToken.None);

        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(detail).IsNotNull();
        await Assert.That(detail!.PromotionCodeDisplayLabel).IsEqualTo("****SAVE2026");
        await Assert.That(detail.TenantId).IsEqualTo(_tenantId);
        await Assert.That(detail.OrganizerOrganizationId).IsNotNull();

        string json = JsonSerializer.Serialize(detail);
        await Assert.That(json).DoesNotContain("TenantId");
        await Assert.That(json).DoesNotContain("ActorId");
        await Assert.That(json).DoesNotContain("OrganizerActorId");
        await Assert.That(json).DoesNotContain("SAVE-PLAINTEXT");
        await Assert.That(json).DoesNotContain("digest");
        await Assert.That(json).DoesNotContain("KeyVersion");
        await Assert.That(json).DoesNotContain("SecretBinding");
        await Assert.That(json).DoesNotContain("LookupKey");
    }

    [Test]
    public async Task QueriesReturnEmptyResultsWhenEventIsNotPlatformManagedForTenant()
    {
        PromotionManagementScenario scenario = CreateScenario();
        Explore.Domain.Event externalEvent = CreatePlatformEvent(scenario.Event.Id);
        externalEvent.ParticipationConfiguration = EventParticipationConfiguration.Create(
            externalEvent.Id,
            _tenantId,
            (int)ParticipationHandlingModeEnum.ExternalManaged,
            (int)AdvanceRegistrationObligationEnum.Required,
            identityAccessModeId: null,
            guestRecoveryPolicy: null,
            UtcNow);
        _events.GetAuthorizationTargetByIdAsync(scenario.Event.Id, Arg.Any<CancellationToken>()).Returns(externalEvent);

        IReadOnlyList<PromotionManagementDto> list = await new ListPromotionManagementQueryHandler(_events, _promotions, _tenant)
            .Handle(new ListPromotionManagementQuery(scenario.Event.Id, scenario.Catalog.Id), CancellationToken.None);
        PromotionManagementDto? detail = await new GetPromotionManagementQueryHandler(_events, _promotions, _tenant)
            .Handle(new GetPromotionManagementQuery(scenario.Event.Id, scenario.Definition.Id), CancellationToken.None);

        await _promotions.DidNotReceive().ListManagementEntriesAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _promotions.DidNotReceive().GetManagementEntryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await Assert.That(list).IsEmpty();
        await Assert.That(detail).IsNull();
    }

    private PromotionManagementScenario CreateScenario()
    {
        Guid eventId = Guid.CreateVersion7();
        Explore.Domain.Event eventTarget = CreatePlatformEvent(eventId);
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(_tenantId, eventId, "EUR", 2);
        PromotionScopeMetadata scope = PromotionScopeMetadata.Create(_tenantId, eventId, catalog.Id, catalog.VersionNumber, catalog.CurrencyCode);
        PromotionDefinition definition = PromotionDefinition.CreateDraft(
            scope,
            "Launch discount",
            PromotionEligibility.ForTicketTypes([Guid.CreateVersion7()]),
            PromotionDiscountRule.BasisPoints("EUR", 1200, maximumDiscountMinor: 3_000),
            UtcNow.AddDays(1),
            UtcNow.AddDays(10),
            totalRedemptionLimit: 50,
            perVerifiedPurchaserLimit: 1);
        definition.Publish(UtcNow.AddHours(-1));
        PromotionCode code = PromotionCode.Create(definition, "SAVE2026", scope);
        return new PromotionManagementScenario(eventTarget, catalog, definition, new PromotionManagementEntry(definition, code));
    }

    private Explore.Domain.Event CreatePlatformEvent(Guid eventId) => new()
    {
        Id = eventId,
        TenantId = _tenantId,
        Title = "Managed event",
        ActorId = Guid.CreateVersion7(),
        Actor = new Actor
        {
            Id = Guid.CreateVersion7(),
            ActorType = new ActorType { FullName = "User", MasterCode = "USER" },
            UserId = Guid.CreateVersion7(),
            Pii = new ActorPii { DisplayName = "Publisher" }
        },
        OrganizerActorId = Guid.CreateVersion7(),
        OrganizerActor = new Actor
        {
            Id = Guid.CreateVersion7(),
            ActorType = new ActorType { FullName = "Organization", MasterCode = "ORG" },
            OrganizationId = Guid.CreateVersion7(),
            Pii = new ActorPii { DisplayName = "Organizer" }
        },
        Tenant = null!,
        VisibilityType = null!,
        EventStatus = null!,
        EventFormat = null!,
        ParticipationConfiguration = EventParticipationConfiguration.Create(
            eventId,
            _tenantId,
            (int)ParticipationHandlingModeEnum.PlatformManaged,
            (int)AdvanceRegistrationObligationEnum.Required,
            (int)IdentityAccessModeEnum.AccountRequired,
            guestRecoveryPolicy: null,
            UtcNow)
    };

    private sealed record PromotionManagementScenario(Explore.Domain.Event Event, EventTicketCatalogVersion Catalog, PromotionDefinition Definition, PromotionManagementEntry Entry);
}
