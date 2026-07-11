// ABOUTME: Unit tests for GetOrganizationSharedContactsQueryHandler — org validation and pagination.
// ABOUTME: Verifies non-org actors and unapproved orgs return empty results.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ContactShareConsent;
using Explore.Application.Features.ContactShareConsents.Handlers.Queries;
using Explore.Application.Features.ContactShareConsents.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.ContactShareConsents.Queries;

public class GetOrganizationSharedContactsQueryHandlerTests
{
    private readonly IEventContactShareConsentRepository _consentRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ILogger<GetOrganizationSharedContactsQueryHandler> _logger;
    private readonly GetOrganizationSharedContactsQueryHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _orgId = Guid.NewGuid();

    public GetOrganizationSharedContactsQueryHandlerTests()
    {
        _consentRepository = Substitute.For<IEventContactShareConsentRepository>();
        _actorRepository = Substitute.For<IActorRepository>();
        _organizationRepository = Substitute.For<IOrganizationRepository>();
        _logger = Substitute.For<ILogger<GetOrganizationSharedContactsQueryHandler>>();

        _handler = new GetOrganizationSharedContactsQueryHandler(
            _consentRepository,
            _actorRepository,
            _organizationRepository,
            _logger);
    }

    [Test]
    public async Task Handle_ActorNotOrganization_ReturnsEmptyResult()
    {
        var query = CreateQuery();
        _actorRepository.GetById(_actorId).Returns(new Actor
        {
            Id = _actorId,
            OrganizationId = null,
            Pii = new ActorPii { DisplayName = "Non-org" },
            ActorType = CreateActorType(),
            Tenant = CreateTenant()
        });

        var result = await _handler.Handle(query, CancellationToken.None);

        await Assert.That(result.TotalCount).IsEqualTo(0);
        await Assert.That(result.Items).IsEmpty();
    }

    [Test]
    public async Task Handle_UnapprovedOrganization_ReturnsEmptyResult()
    {
        var query = CreateQuery();
        SetupActor();
        _organizationRepository.GetById(_orgId).Returns(CreateOrganizationEntity(_orgId, approved: false));

        var result = await _handler.Handle(query, CancellationToken.None);

        await Assert.That(result.TotalCount).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_ApprovedOrganization_ReturnsContacts()
    {
        var query = CreateQuery();
        SetupValidOrgChain();

        var consents = Enumerable.Range(0, 3).Select(i => new EventContactShareConsent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            UserId = Guid.NewGuid(),
            RecipientActorId = _actorId,
            SourceEventId = Guid.NewGuid(),
            SourceEvent = new Explore.Domain.Event
            {
                Id = Guid.NewGuid(),
                Title = $"Event {i}",
                Actor = CreateActorEntity(_actorId, _orgId),
                Tenant = CreateTenant(),
                VisibilityType = new VisibilityType { MasterCode = "PUBLIC", FullName = "Public" },
                EventStatus = new EventStatus { MasterCode = "PUBLISHED", FullName = "Published" },
                EventFormat = new EventFormat { MasterCode = "IN_PERSON", FullName = "In Person" }
            },
            PurposeCode = ConsentPurposeCodes.OrganizerFutureCommunications,
            Status = ConsentStatus.Granted,
            EmailSnapshot = $"user{i}@example.com",
            EmailNormalizedSnapshot = $"user{i}@example.com",
            ConsentTextSnapshot = "text",
            ConsentUiVersion = "v1",
            GrantedAt = DateTime.UtcNow
        }).ToList();

        _consentRepository.GetGrantedForRecipient(_tenantId, _actorId, null, null, 1, 20)
            .Returns((consents, 3));

        var result = await _handler.Handle(query, CancellationToken.None);

        await Assert.That(result.TotalCount).IsEqualTo(3);
        await Assert.That(result.Items.Count).IsEqualTo(3);
        await Assert.That(result.Items[0].Email).IsEqualTo("user0@example.com");
    }

    [Test]
    public async Task SecureRequest_UsesOrganizationIdAndTenantId_ForAuthorizationContext()
    {
        var query = CreateQuery();
        var secureRequest = (ISecureRequest)query;

        await Assert.That(secureRequest.ResourceId).IsEqualTo(_orgId.ToString());
        await Assert.That(secureRequest.ResourceAttributes!["organizationId"]).IsEqualTo(_orgId);
        await Assert.That(secureRequest.ResourceAttributes!["tenantId"]).IsEqualTo(_tenantId);
    }

    #region Helpers

    private static Tenant CreateTenant() => new()
    {
        FullName = "Test Tenant",
        Slug = "test-tenant",
        TenantStatus = new TenantStatus { MasterCode = "ACTIVE", FullName = "Active" }
    };

    private static ActorType CreateActorType() => new()
    {
        FullName = "Organization",
        MasterCode = "ORG"
    };

    private static Actor CreateActorEntity(Guid id, Guid? orgId = null) => new()
    {
        Id = id,
        OrganizationId = orgId,
        Pii = new ActorPii { DisplayName = "Org Actor" },
        ActorType = CreateActorType(),
        Tenant = CreateTenant()
    };

    private static Organization CreateOrganizationEntity(Guid id, bool approved = true) => new()
    {
        Id = id,
        ApprovalStatusId = approved ? (int)ApprovalStatusEnum.Approved : 1,
        Pii = new OrganizationPii { FullName = approved ? "Approved Org" : "Unapproved Org" },
        ApprovalStatus = new ApprovalStatus
        {
            MasterCode = approved ? "APPROVED" : "PENDING",
            FullName = approved ? "Approved" : "Pending"
        },
        Tenant = CreateTenant()
    };

    private GetOrganizationSharedContactsQuery CreateQuery()
    {
        return new GetOrganizationSharedContactsQuery
        {
            RecipientActorId = _actorId,
            OrganizationId = _orgId,
            TenantId = _tenantId,
            PageNumber = 1,
            PageSize = 20
        };
    }

    private void SetupActor()
    {
        _actorRepository.GetById(_actorId).Returns(CreateActorEntity(_actorId, _orgId));
    }

    private void SetupValidOrgChain()
    {
        SetupActor();
        _organizationRepository.GetById(_orgId).Returns(CreateOrganizationEntity(_orgId, approved: true));
    }

    #endregion
}
