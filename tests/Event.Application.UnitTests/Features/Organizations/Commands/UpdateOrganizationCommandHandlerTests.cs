// ABOUTME: Unit tests for grouped Organization profile update command handling.
// ABOUTME: Covers validation, OrgAdmin authorization, optimistic concurrency, clear semantics, and cache invalidation.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization;
using Explore.Application.Exceptions;
using Explore.Application.Features.Organizations.Handlers.Commands;
using Explore.Application.Features.Organizations.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Organizations.Commands;

public class UpdateOrganizationCommandHandlerTests
{
    private readonly IOrganizationRepository _organizationRepository = Substitute.For<IOrganizationRepository>();
    private readonly IOrganizationMemberRepository _organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly UpdateOrganizationCommandHandler _handler;

    public UpdateOrganizationCommandHandlerTests()
    {
        _handler = new UpdateOrganizationCommandHandler(
            _organizationRepository,
            _organizationMemberRepository,
            _cache);
    }

    [Test]
    public async Task Handle_WhenWrapperHasNoGroups_ReturnsValidationFailureAndDoesNotSave()
    {
        var result = await _handler.Handle(new UpdateOrganizationCommand
        {
            OrganizationId = Guid.CreateVersion7(),
            UserId = Guid.CreateVersion7().ToString(),
            ExpectedConcurrencyStamp = Guid.CreateVersion7(),
            UpdateOrganizationDto = new UpdateOrganizationDto()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("At least one organization update group must be provided.");
        await _organizationRepository.DidNotReceive().Update(Arg.Any<Organization>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenRequesterIsNotOrgAdmin_ReturnsAuthorizationFailureAndDoesNotSave()
    {
        var userId = Guid.CreateVersion7();
        var organization = CreateOrganization();
        _organizationRepository.GetById(organization.Id).Returns(organization);
        _organizationMemberRepository.GetMembersByOrganizationId(organization.Id)
            .Returns([CreateMember(organization, userId, RoleEnum.OrgMember)]);

        await Assert.ThrowsAsync<AuthorizationException>(() => _handler.Handle(new UpdateOrganizationCommand
        {
            OrganizationId = organization.Id,
            UserId = userId.ToString(),
            ExpectedConcurrencyStamp = organization.ConcurrencyStamp,
            UpdateOrganizationDto = new UpdateOrganizationDto
            {
                FullName = new UpdateOrganizationFullNameDto { Value = "Updated Organization" }
            }
        }, CancellationToken.None));

        await _organizationRepository.DidNotReceive().Update(Arg.Any<Organization>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenExpectedConcurrencyStampIsStale_ThrowsConflictAndDoesNotSave()
    {
        var userId = Guid.CreateVersion7();
        var organization = CreateOrganization();
        _organizationRepository.GetById(organization.Id).Returns(organization);
        _organizationMemberRepository.GetMembersByOrganizationId(organization.Id)
            .Returns([CreateMember(organization, userId, RoleEnum.OrgAdmin)]);

        await Assert.That(async () => await _handler.Handle(new UpdateOrganizationCommand
        {
            OrganizationId = organization.Id,
            UserId = userId.ToString(),
            ExpectedConcurrencyStamp = Guid.CreateVersion7(),
            UpdateOrganizationDto = new UpdateOrganizationDto
            {
                FullName = new UpdateOrganizationFullNameDto { Value = "Updated Organization" }
            }
        }, CancellationToken.None)).Throws<ConcurrencyConflictException>();

        await _organizationRepository.DidNotReceive().Update(Arg.Any<Organization>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenSingleGroupIsPresent_UpdatesOnlyThatFieldAndInvalidatesDetailCache()
    {
        var userId = Guid.CreateVersion7();
        var organization = CreateOrganization();
        _organizationRepository.GetById(organization.Id).Returns(organization);
        _organizationMemberRepository.GetMembersByOrganizationId(organization.Id)
            .Returns([CreateMember(organization, userId, RoleEnum.OrgAdmin)]);

        var result = await _handler.Handle(new UpdateOrganizationCommand
        {
            OrganizationId = organization.Id,
            UserId = userId.ToString(),
            ExpectedConcurrencyStamp = organization.ConcurrencyStamp,
            UpdateOrganizationDto = new UpdateOrganizationDto
            {
                FullName = new UpdateOrganizationFullNameDto { Value = "Updated Organization" }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(organization.FullName).IsEqualTo("Updated Organization");
        await Assert.That(organization.Email).IsEqualTo("existing@example.com");
        await _organizationRepository.Received(1).Update(organization);
        await _cache.Received(1).RemoveAsync($"organization:detail:{organization.Id}", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenWebsiteUrlExplicitlyClears_SetsWebsiteUrlToNull()
    {
        var userId = Guid.CreateVersion7();
        var organization = CreateOrganization();
        _organizationRepository.GetById(organization.Id).Returns(organization);
        _organizationMemberRepository.GetMembersByOrganizationId(organization.Id)
            .Returns([CreateMember(organization, userId, RoleEnum.OrgAdmin)]);

        var result = await _handler.Handle(new UpdateOrganizationCommand
        {
            OrganizationId = organization.Id,
            UserId = userId.ToString(),
            ExpectedConcurrencyStamp = organization.ConcurrencyStamp,
            UpdateOrganizationDto = new UpdateOrganizationDto
            {
                WebsiteUrl = new UpdateOrganizationWebsiteUrlDto
                {
                    Value = OptionalUpdate<string?>.Set(null)
                }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(organization.WebsiteUrl).IsNull();
        await _organizationRepository.Received(1).Update(organization);
    }

    [Test]
    public async Task Handle_WhenWebsiteUrlGroupHasNoFieldOperation_ReturnsValidationFailure()
    {
        var result = await _handler.Handle(new UpdateOrganizationCommand
        {
            OrganizationId = Guid.CreateVersion7(),
            UserId = Guid.CreateVersion7().ToString(),
            ExpectedConcurrencyStamp = Guid.CreateVersion7(),
            UpdateOrganizationDto = new UpdateOrganizationDto
            {
                WebsiteUrl = new UpdateOrganizationWebsiteUrlDto()
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("WebsiteUrl group must include Value.");
        await _organizationRepository.DidNotReceive().Update(Arg.Any<Organization>());
    }

    private static Organization CreateOrganization()
    {
        return new Organization
        {
            Id = Guid.CreateVersion7(),
            ConcurrencyStamp = Guid.CreateVersion7(),
            Pii = new OrganizationPii
            {
                FullName = "Existing Organization",
                Email = "existing@example.com",
                Country = "Belgium",
                City = "Brussels",
                Postcode = "1000",
                Address = "Existing Street 1"
            },
            WebsiteUrl = "https://example.com"
        };
    }

    private static OrganizationMember CreateMember(Organization organization, Guid userId, RoleEnum role)
    {
        Guid tenantId = Guid.CreateVersion7();
        var participation = new OrganizationTenant
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organization.Id,
            Organization = organization,
            TenantId = tenantId,
            Tenant = null!,
            ApprovalStatusId = (int)ApprovalStatusEnum.Pending,
            ApprovalStatus = null!
        };
        organization.TenantParticipations.Add(participation);

        return new OrganizationMember
        {
            Id = Guid.CreateVersion7(),
            OrganizationTenantId = participation.Id,
            OrganizationTenant = participation,
            UserId = userId,
            User = null!,
            RoleId = (int)role,
            Role = null!,
            TenantId = tenantId,
            Tenant = null!
        };
    }
}
