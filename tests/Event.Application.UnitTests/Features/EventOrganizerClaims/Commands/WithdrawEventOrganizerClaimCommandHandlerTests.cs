// ABOUTME: Verifies organizer-claim withdrawal remains available as an ownership-based revocation.
// ABOUTME: Covers revoked claimant eligibility and unrelated-user denial.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventOrganizerClaims.Handlers.Commands;
using Explore.Application.Features.EventOrganizerClaims.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventOrganizerClaims.Commands;

[Category("EventActorEligibility")]
public sealed class WithdrawEventOrganizerClaimCommandHandlerTests
{
    [Test]
    public async Task Handle_ControllerCanWithdrawAfterClaimantEligibilityIsRevoked()
    {
        var (result, claim, claimRepository) = await WithdrawAsync(canControl: true);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(claim.StatusId).IsEqualTo((int)EventOrganizerClaimStatusEnum.Withdrawn);
        await claimRepository.Received(1).Update(claim);
    }

    [Test]
    public async Task Handle_UnrelatedUserCannotWithdrawAfterClaimantEligibilityIsRevoked()
    {
        var (result, claim, claimRepository) = await WithdrawAsync(canControl: false);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(claim.StatusId).IsEqualTo((int)EventOrganizerClaimStatusEnum.Pending);
        await claimRepository.DidNotReceive().Update(Arg.Any<EventOrganizerClaim>());
    }

    private static async Task<(
        BaseCommandResponse<Guid> Result,
        EventOrganizerClaim Claim,
        IEventOrganizerClaimRepository ClaimRepository)> WithdrawAsync(bool canControl)
    {
        var tenantId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var organizationId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var actor = new Actor
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            ActorTypeId = (int)ActorTypeEnum.Organization,
            ActorType = null!,
            IsSuspended = true,
            Pii = new ActorPii { DisplayName = "Revoked organization claimant" }
        };
        var claim = EventOrganizerClaim.CreatePending(
            tenantId,
            eventId,
            actor.Id,
            "DOMAIN_EMAIL",
            "evidence-reference",
            DateTime.UtcNow);
        claim.ConcurrencyStamp = Guid.CreateVersion7();
        var claimRepository = Substitute.For<IEventOrganizerClaimRepository>();
        claimRepository.GetForUpdateAsync(claim.Id, Arg.Any<CancellationToken>()).Returns(claim);
        var actorRepository = Substitute.For<IActorRepository>();
        actorRepository.GetActorWithDetails(actor.Id, Arg.Any<CancellationToken>()).Returns(actor);
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        organizationMemberRepository.HasPermissionInOrganization(
                organizationId,
                userId,
                PermissionCodes.EventCreate)
            .Returns(canControl);
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        var handler = new WithdrawEventOrganizerClaimCommandHandler(
            claimRepository,
            actorRepository,
            organizationMemberRepository,
            groupMemberRepository,
            tenantContext,
            currentUser);

        var result = await handler.Handle(new WithdrawEventOrganizerClaimCommand
        {
            EventId = eventId,
            ClaimId = claim.Id,
            ExpectedConcurrencyStamp = claim.ConcurrencyStamp
        }, CancellationToken.None);

        return (result, claim, claimRepository);
    }
}
