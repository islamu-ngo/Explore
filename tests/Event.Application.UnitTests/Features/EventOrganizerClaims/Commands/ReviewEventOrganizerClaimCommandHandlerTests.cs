// ABOUTME: Verifies organizer-claim approval changes event authority transactionally and only once.
// ABOUTME: Covers exact retry idempotency after an approval commit response is lost.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventOrganizerClaim;
using Explore.Application.Features.EventOrganizerClaims.Handlers.Commands;
using Explore.Application.Features.EventOrganizerClaims.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventOrganizerClaims.Commands;

public sealed class ReviewEventOrganizerClaimCommandHandlerTests
{
    [Test]
    public async Task Handle_ApprovalRetried_ReturnsSuccessWithoutRepeatingAuthorityWrite()
    {
        var tenantId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var claimantActorId = Guid.CreateVersion7();
        var reviewerUserId = Guid.CreateVersion7();
        var concurrencyStamp = Guid.CreateVersion7();
        var claim = EventOrganizerClaim.CreatePending(
            tenantId,
            eventId,
            claimantActorId,
            "DOMAIN_EMAIL",
            "evidence-reference",
            DateTime.UtcNow);
        claim.ConcurrencyStamp = concurrencyStamp;
        var @event = CreateEvent(tenantId, eventId);
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetById(eventId).Returns(@event);
        var claimRepository = Substitute.For<IEventOrganizerClaimRepository>();
        claimRepository.GetForUpdateAsync(claim.Id, Arg.Any<CancellationToken>()).Returns(claim);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>()(CancellationToken.None));
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(reviewerUserId);
        var handler = new ReviewEventOrganizerClaimCommandHandler(
            eventRepository,
            claimRepository,
            unitOfWork,
            tenantContext,
            currentUser);
        var command = new ReviewEventOrganizerClaimCommand
        {
            EventId = eventId,
            ClaimId = claim.Id,
            Review = new ReviewEventOrganizerClaimDto
            {
                Decision = EventOrganizerClaimReviewDecision.Approve,
                ReasonCode = "VERIFIED_CONTROL",
                ExpectedConcurrencyStamp = concurrencyStamp
            }
        };

        var first = await handler.Handle(command, CancellationToken.None);
        var retry = await handler.Handle(command, CancellationToken.None);

        await Assert.That(first.Success).IsTrue();
        await Assert.That(retry.Success).IsTrue();
        await Assert.That(@event.OrganizerActorId).IsEqualTo(claimantActorId);
        await claimRepository.Received(1).UpdateApprovalAsync(claim, @event, Arg.Any<CancellationToken>());
    }

    private static Explore.Domain.Event CreateEvent(Guid tenantId, Guid eventId) => new()
    {
        Id = eventId,
        TenantId = tenantId,
        Title = "Community event",
        EventProvenanceTypeId = (int)EventProvenanceTypeEnum.CommunityReported,
        SubmittedByUserId = Guid.CreateVersion7(),
        Actor = null!,
        Tenant = null!,
        VisibilityType = null!,
        EventStatus = null!,
        EventFormat = null!
    };
}
