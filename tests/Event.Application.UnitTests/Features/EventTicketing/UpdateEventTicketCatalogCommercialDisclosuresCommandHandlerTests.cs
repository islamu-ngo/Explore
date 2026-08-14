// ABOUTME: Tests draft ticket catalog commercial disclosure command behavior.
// ABOUTME: Covers tenant/event masking, domain normalization, validation, and persistence timing.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventTicketing.Handlers.Commands;
using Explore.Application.Features.EventTicketing.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;
using DomainEvent = Explore.Domain.Event;

namespace Event.Application.UnitTests.Features.EventTicketing;

[Category("Phase43Ticketing")]
public sealed class UpdateEventTicketCatalogCommercialDisclosuresCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IEventTicketCatalogRepository _catalogs = Substitute.For<IEventTicketCatalogRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();

    public UpdateEventTicketCatalogCommercialDisclosuresCommandHandlerTests()
    {
        _tenant.TenantId.Returns(_tenantId);
    }

    [Test]
    public async Task Handle_WhenDraftExists_NormalizesDisclosuresAndSaves()
    {
        EventTicketCatalogVersion draft = EventTicketCatalogVersion.Create(_tenantId, _eventId, "USD", 1);
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>()).Returns(CreatePlatformEvent(_tenantId, _eventId));
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(draft);

        var result = await CreateHandler().Handle(new UpdateEventTicketCatalogCommercialDisclosuresCommand
        {
            EventId = _eventId,
            MerchantDisclosureText = " Merchant ",
            RefundPolicyDisclosureText = " Refund ",
            SupportContactDisclosureText = " Support "
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(draft.Id);
        await Assert.That(draft.MerchantDisclosureText).IsEqualTo("Merchant");
        await Assert.That(draft.RefundPolicyDisclosureText).IsEqualTo("Refund");
        await Assert.That(draft.SupportContactDisclosureText).IsEqualTo("Support");
        await _catalogs.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenEventIsNotPlatformManaged_ReturnsNotFoundWithoutDraftRead()
    {
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>())
            .Returns(CreateEvent(_tenantId, _eventId, ParticipationHandlingModeEnum.ExternalManaged));

        var result = await CreateHandler().Handle(new UpdateEventTicketCatalogCommercialDisclosuresCommand { EventId = _eventId }, CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_not_found");
        await _catalogs.DidNotReceive().GetDraftCatalogForUpdateAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _catalogs.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenDisclosureIsTooLong_ReturnsValidationFailureWithoutSave()
    {
        EventTicketCatalogVersion draft = EventTicketCatalogVersion.Create(_tenantId, _eventId, "USD", 1);
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>()).Returns(CreatePlatformEvent(_tenantId, _eventId));
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(draft);

        var result = await CreateHandler().Handle(new UpdateEventTicketCatalogCommercialDisclosuresCommand
        {
            EventId = _eventId,
            MerchantDisclosureText = new string('x', EventTicketCatalogVersion.MaxCommercialDisclosureTextLength + 1)
        }, CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_validation_failed");
        await _catalogs.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private UpdateEventTicketCatalogCommercialDisclosuresCommandHandler CreateHandler() => new(_events, _catalogs, _tenant);

    private DomainEvent CreatePlatformEvent(Guid tenantId, Guid eventId) =>
        CreateEvent(tenantId, eventId, ParticipationHandlingModeEnum.PlatformManaged);

    private static DomainEvent CreateEvent(Guid tenantId, Guid eventId, ParticipationHandlingModeEnum mode) => new()
    {
        Id = eventId,
        TenantId = tenantId,
        ActorId = Guid.CreateVersion7(),
        Title = "Ticketing event",
        Actor = null!,
        Tenant = null!,
        VisibilityType = null!,
        EventStatus = null!,
        EventFormat = null!,
        ParticipationConfiguration = EventParticipationConfiguration.Create(
            eventId,
            tenantId,
            (int)mode,
            (int)AdvanceRegistrationObligationEnum.Required,
            mode == ParticipationHandlingModeEnum.PlatformManaged ? (int)IdentityAccessModeEnum.AccountRequired : null,
            guestRecoveryPolicy: null,
            DateTime.UtcNow)
    };
}
