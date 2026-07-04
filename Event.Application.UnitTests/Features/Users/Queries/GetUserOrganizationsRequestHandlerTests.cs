// ABOUTME: Unit tests for current-user scoping in the user organizations query handler.
// ABOUTME: Proves forged route user IDs cannot read another user's organization memberships.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization;
using Explore.Application.Exceptions;
using Explore.Application.Features.Users.Handlers.Queries;
using Explore.Application.Features.Users.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Users.Queries;

public sealed class GetUserOrganizationsRequestHandlerTests
{
    private readonly IOrganizationMemberRepository _organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetUserOrganizationsRequestHandler _handler;

    public GetUserOrganizationsRequestHandlerTests()
    {
        _handler = new GetUserOrganizationsRequestHandler(
            _organizationMemberRepository,
            _mapper,
            _currentUserService);
    }

    [Test]
    public async Task Handle_WhenRequestedUserDiffersFromCurrentUser_ThrowsAuthorizationException()
    {
        _currentUserService.UserId.Returns(Guid.CreateVersion7());

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _handler.Handle(new GetUserOrganizationsRequest { UserId = Guid.CreateVersion7() }, CancellationToken.None));

        await _organizationMemberRepository.DidNotReceiveWithAnyArgs().GetMembershipsByUser(default);
    }

    [Test]
    public async Task Handle_WhenCurrentUserIsMissing_ThrowsAuthorizationException()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _handler.Handle(new GetUserOrganizationsRequest { UserId = Guid.CreateVersion7() }, CancellationToken.None));

        await _organizationMemberRepository.DidNotReceiveWithAnyArgs().GetMembershipsByUser(default);
    }

    [Test]
    public async Task Handle_WhenRequestedUserMatchesCurrentUser_MapsMembershipOrganizations()
    {
        var userId = Guid.CreateVersion7();
        var organization = new Organization
        {
            Id = Guid.CreateVersion7(),
            Pii = null!,
            ApprovalStatus = null!,
            Tenant = null!
        };
        var membership = new OrganizationMember
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            User = null!,
            OrganizationId = organization.Id,
            Organization = organization,
            RoleId = (int)RoleEnum.OrgAdmin,
            Role = null!,
            Tenant = null!
        };
        var dto = new OrganizationListDto
        {
            Id = organization.Id,
            FullName = "Organization",
            Email = "org@example.test",
            Country = "BE",
            City = "Brussels",
            Postcode = "1000",
            Address = "Main Street",
            ApprovalStatusFullName = "Approved"
        };

        _currentUserService.UserId.Returns(userId);
        _organizationMemberRepository.GetMembershipsByUser(userId).Returns([membership]);
        _mapper.Map<OrganizationListDto>(organization).Returns(dto);

        var result = await _handler.Handle(new GetUserOrganizationsRequest { UserId = userId }, CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].CurrentUserRole).IsEqualTo(RoleEnum.OrgAdmin);
        await _organizationMemberRepository.Received(1).GetMembershipsByUser(userId);
    }
}
