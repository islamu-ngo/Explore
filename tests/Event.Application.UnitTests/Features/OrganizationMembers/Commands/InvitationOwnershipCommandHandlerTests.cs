// ABOUTME: Tests organization invitation accept/decline ownership containment.
// ABOUTME: Proves only the invitee-bound membership row can be accepted or declined.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.OrganizationMembers.Handlers.Commands;
using Explore.Application.Features.OrganizationMembers.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.OrganizationMembers.Commands;

public sealed class InvitationOwnershipCommandHandlerTests
{
    [Test]
    public async Task Accept_WhenInvitationBelongsToRequester_SucceedsWithoutMembershipMutation()
    {
        var requesterId = Guid.CreateVersion7();
        var invitation = CreateInvitation(requesterId);
        var repository = Substitute.For<IOrganizationMemberRepository>();
        repository.GetById(invitation.Id).Returns(invitation);
        var handler = new AcceptInvitationCommandHandler(repository);

        var result = await handler.Handle(new AcceptInvitationCommand
        {
            InvitationId = invitation.Id,
            UserId = requesterId
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(invitation.Id);
        await Assert.That(invitation.UserId).IsEqualTo(requesterId);
        await repository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Test]
    public async Task Accept_WhenInvitationBelongsToAnotherUser_DeniesWithoutMembershipMutation()
    {
        var invitation = CreateInvitation(Guid.CreateVersion7());
        var repository = Substitute.For<IOrganizationMemberRepository>();
        repository.GetById(invitation.Id).Returns(invitation);
        var handler = new AcceptInvitationCommandHandler(repository);

        var result = await handler.Handle(new AcceptInvitationCommand
        {
            InvitationId = invitation.Id,
            UserId = Guid.CreateVersion7()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Invitation not found");
        await Assert.That(result.Id).IsEqualTo(default(Guid));
        await repository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Test]
    public async Task Accept_WhenInvitationIsUnbound_DeniesWithoutBindingRequester()
    {
        var invitation = CreateInvitation(Guid.Empty);
        var repository = Substitute.For<IOrganizationMemberRepository>();
        repository.GetById(invitation.Id).Returns(invitation);
        var handler = new AcceptInvitationCommandHandler(repository);

        var result = await handler.Handle(new AcceptInvitationCommand
        {
            InvitationId = invitation.Id,
            UserId = Guid.CreateVersion7()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Invitation not found");
        await Assert.That(invitation.UserId).IsEqualTo(Guid.Empty);
        await repository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Test]
    public async Task Decline_WhenInvitationBelongsToRequester_DeletesInvitation()
    {
        var requesterId = Guid.CreateVersion7();
        var invitation = CreateInvitation(requesterId);
        var repository = Substitute.For<IOrganizationMemberRepository>();
        repository.GetById(invitation.Id).Returns(invitation);
        var handler = new DeclineInvitationCommandHandler(repository);

        var result = await handler.Handle(new DeclineInvitationCommand
        {
            InvitationId = invitation.Id,
            UserId = requesterId
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(invitation.Id);
        await repository.Received(1).Delete(invitation);
    }

    [Test]
    public async Task Decline_WhenInvitationBelongsToAnotherUser_DeniesWithoutDeletingInvitation()
    {
        var invitation = CreateInvitation(Guid.CreateVersion7());
        var repository = Substitute.For<IOrganizationMemberRepository>();
        repository.GetById(invitation.Id).Returns(invitation);
        var handler = new DeclineInvitationCommandHandler(repository);

        var result = await handler.Handle(new DeclineInvitationCommand
        {
            InvitationId = invitation.Id,
            UserId = Guid.CreateVersion7()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Invitation not found");
        await repository.DidNotReceiveWithAnyArgs().Delete(default!);
    }

    [Test]
    public async Task Decline_WhenInvitationIsUnbound_DeniesWithoutDeletingInvitation()
    {
        var invitation = CreateInvitation(Guid.Empty);
        var repository = Substitute.For<IOrganizationMemberRepository>();
        repository.GetById(invitation.Id).Returns(invitation);
        var handler = new DeclineInvitationCommandHandler(repository);

        var result = await handler.Handle(new DeclineInvitationCommand
        {
            InvitationId = invitation.Id,
            UserId = Guid.CreateVersion7()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Invitation not found");
        await repository.DidNotReceiveWithAnyArgs().Delete(default!);
    }

    private static OrganizationMember CreateInvitation(Guid userId)
    {
        var organization = new Organization
        {
            Id = Guid.CreateVersion7(),
            Pii = null!
        };
        var organizationTenant = new OrganizationTenant
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organization.Id,
            Organization = organization,
            TenantId = Guid.CreateVersion7(),
            Tenant = null!,
            ApprovalStatus = null!
        };

        return new OrganizationMember
        {
            Id = Guid.CreateVersion7(),
            OrganizationTenantId = organizationTenant.Id,
            OrganizationTenant = organizationTenant,
            UserId = userId,
            User = null!,
            RoleId = (int)RoleEnum.OrgMember,
            Role = null!,
            TenantId = organizationTenant.TenantId,
            Tenant = null!
        };
    }
}
