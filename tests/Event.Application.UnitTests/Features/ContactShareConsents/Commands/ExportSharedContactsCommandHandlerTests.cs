// ABOUTME: Unit tests for ExportSharedContactsCommandHandler — CSV/TSV generation and audit trail.
// ABOUTME: Covers format validation, org approval checks, file generation, and audit record creation.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ContactShareConsent;
using Explore.Application.Features.ContactShareConsents.Handlers.Commands;
using Explore.Application.Features.ContactShareConsents.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.ContactShareConsents.Commands;

public class ExportSharedContactsCommandHandlerTests
{
    private readonly IEventContactShareConsentRepository _consentRepository;
    private readonly IEventContactShareExportRepository _exportRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ILogger<ExportSharedContactsCommandHandler> _logger;
    private readonly ExportSharedContactsCommandHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public ExportSharedContactsCommandHandlerTests()
    {
        _consentRepository = Substitute.For<IEventContactShareConsentRepository>();
        _exportRepository = Substitute.For<IEventContactShareExportRepository>();
        _actorRepository = Substitute.For<IActorRepository>();
        _organizationRepository = Substitute.For<IOrganizationRepository>();
        _logger = Substitute.For<ILogger<ExportSharedContactsCommandHandler>>();

        _handler = new ExportSharedContactsCommandHandler(
            _consentRepository,
            _exportRepository,
            _actorRepository,
            _organizationRepository,
            _logger);
    }

    #region Format validation

    [Test]
    public async Task Handle_InvalidFormat_ReturnsFailure()
    {
        var command = CreateCommand(format: "json");

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).Contains("Invalid export format");
    }

    #endregion

    #region Actor/org validation

    [Test]
    public async Task Handle_ActorNotOrganization_ReturnsFailure()
    {
        var command = CreateCommand();
        _actorRepository.GetById(_actorId).Returns(new Actor
        {
            Id = _actorId,
            OrganizationId = null,
            Pii = new ActorPii { DisplayName = "User Actor" },
            ActorType = CreateActorType()
        });

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).Contains("not an organisation");
    }

    [Test]
    public async Task Handle_UnapprovedOrganization_ReturnsFailure()
    {
        var command = CreateCommand();
        SetupActor();
        _organizationRepository.GetById(_orgId).Returns(CreateOrganizationEntity(_orgId, approved: false));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).Contains("not approved");
    }

    [Test]
    public async Task Handle_ActorOrganizationMismatch_ReturnsFailure()
    {
        var command = CreateCommand();
        _actorRepository.GetById(_actorId).Returns(CreateActorEntity(_actorId, Guid.CreateVersion7()));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await _exportRepository.DidNotReceive().Create(Arg.Any<EventContactShareExport>());
    }

    #endregion

    #region CSV export

    [Test]
    public async Task Handle_CsvFormat_ReturnsValidCsvContent()
    {
        var command = CreateCommand(format: "csv");
        SetupValidOrgChain();

        var consents = CreateGrantedConsents(2);
        _consentRepository.GetGrantedForExport(
            _tenantId, _actorId, null, ConsentPurposeCodes.OrganizerFutureCommunications).Returns(consents);
        _exportRepository.Create(Arg.Any<EventContactShareExport>())
            .Returns(ci => ci.Arg<EventContactShareExport>());

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();

        var dto = result.Id!;
        await Assert.That(dto.Format).IsEqualTo("csv");
        await Assert.That(dto.RowCount).IsEqualTo(2);
        await Assert.That(dto.ContentType).IsEqualTo("text/csv");

        var content = System.Text.Encoding.UTF8.GetString(dto.FileContent!.Value.Span);
        await Assert.That(content).Contains("Email,GrantedAtUtc,EventId,EventTitle,OrganizationId,OrganizationName,PurposeCode");
        await Assert.That(content).Contains(consents[0].EmailSnapshot);
        await Assert.That(content).Contains(consents[1].EmailSnapshot);
    }

    [Test]
    public async Task Handle_CsvFormat_SanitizesFormulaCellsAndFileName()
    {
        var command = CreateCommand(format: " CSV ");
        SetupActor();
        _organizationRepository.GetById(_orgId)
            .Returns(CreateOrganizationEntity(_orgId, fullName: "=Approved/Org"));

        var consents = CreateGrantedConsents(1, "=cmd|' /C calc'!A0");
        _consentRepository.GetGrantedForExport(
            _tenantId, _actorId, null, ConsentPurposeCodes.OrganizerFutureCommunications).Returns(consents);
        _exportRepository.Create(Arg.Any<EventContactShareExport>())
            .Returns(ci => ci.Arg<EventContactShareExport>());

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id!.Format).IsEqualTo("csv");
        await Assert.That(result.Id.FileName).Contains("shared-contacts-approved-org-");

        var content = System.Text.Encoding.UTF8.GetString(result.Id.FileContent!.Value.Span);
        await Assert.That(content).Contains("'=cmd|' /C calc'!A0");
        await Assert.That(content).Contains("'=Approved/Org");
    }

    #endregion

    #region TSV export

    [Test]
    public async Task Handle_TsvFormat_ReturnsValidTsvContent()
    {
        var command = CreateCommand(format: "tsv");
        SetupValidOrgChain();

        var consents = CreateGrantedConsents(1);
        _consentRepository.GetGrantedForExport(
            _tenantId, _actorId, null, ConsentPurposeCodes.OrganizerFutureCommunications).Returns(consents);
        _exportRepository.Create(Arg.Any<EventContactShareExport>())
            .Returns(ci => ci.Arg<EventContactShareExport>());

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();

        var dto = result.Id!;
        await Assert.That(dto.Format).IsEqualTo("tsv");
        await Assert.That(dto.ContentType).IsEqualTo("text/tab-separated-values");

        var content = System.Text.Encoding.UTF8.GetString(dto.FileContent!.Value.Span);
        await Assert.That(content).Contains("Email\tGrantedAtUtc\tEventId\tEventTitle\tOrganizationId\tOrganizationName\tPurposeCode");
    }

    #endregion

    #region Export audit trail

    [Test]
    public async Task Handle_SuccessfulExport_RecordsAuditTrail()
    {
        var command = CreateCommand();
        SetupValidOrgChain();

        var consents = CreateGrantedConsents(3);
        _consentRepository.GetGrantedForExport(
            _tenantId, _actorId, null, ConsentPurposeCodes.OrganizerFutureCommunications).Returns(consents);
        _exportRepository.Create(Arg.Any<EventContactShareExport>())
            .Returns(ci => ci.Arg<EventContactShareExport>());

        await _handler.Handle(command, CancellationToken.None);

        await _exportRepository.Received(1).Create(Arg.Is<EventContactShareExport>(e =>
            e.TenantId == _tenantId &&
            e.RecipientActorId == _actorId &&
            e.ExportedByUserId == _userId &&
            e.Format == "csv" &&
            e.RowCount == 3 &&
            e.Items.Count == 3));
    }

    [Test]
    public async Task Handle_EmptyConsents_StillRecordsAuditWithZeroRows()
    {
        var command = CreateCommand();
        SetupValidOrgChain();
        _consentRepository.GetGrantedForExport(
            _tenantId, _actorId, null, ConsentPurposeCodes.OrganizerFutureCommunications).Returns([]);
        _exportRepository.Create(Arg.Any<EventContactShareExport>())
            .Returns(ci => ci.Arg<EventContactShareExport>());

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id!.RowCount).IsEqualTo(0);
        await _exportRepository.Received(1).Create(Arg.Is<EventContactShareExport>(e => e.RowCount == 0));
    }

    [Test]
    public async Task SecureRequest_UsesOrganizationIdAndTenantId_ForAuthorizationContext()
    {
        var command = CreateCommand();
        var secureRequest = (ISecureRequest)command;

        await Assert.That(secureRequest.ResourceId).IsEqualTo(_orgId.ToString());
        await Assert.That(secureRequest.AuthorizationFacts)
            .IsEqualTo(new ContactShareAuthorizationFacts(_tenantId, _orgId));
    }

    #endregion

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
        ActorType = CreateActorType()
    };

    private Organization CreateOrganizationEntity(Guid id, bool approved = true, string? fullName = null)
    {
        var organization = new Organization
        {
            Id = id,
            Pii = new OrganizationPii { FullName = fullName ?? (approved ? "Approved Org" : "Unapproved Org") }
        };
        Tenant tenant = CreateTenant();
        tenant.Id = _tenantId;
        organization.TenantParticipations.Add(new OrganizationTenant
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = id,
            Organization = organization,
            TenantId = tenant.Id,
            Tenant = tenant,
            ApprovalStatusId = approved ? (int)ApprovalStatusEnum.Approved : 1,
            ApprovalStatus = new ApprovalStatus
            {
                MasterCode = approved ? "APPROVED" : "PENDING",
                FullName = approved ? "Approved" : "Pending"
            }
        });
        return organization;
    }

    private ExportSharedContactsCommand CreateCommand(string format = "csv")
    {
        return new ExportSharedContactsCommand
        {
            RecipientActorId = _actorId,
            OrganizationId = _orgId,
            TenantId = _tenantId,
            ExportedByUserId = _userId,
            Format = format
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

    private List<EventContactShareConsent> CreateGrantedConsents(int count, string? firstEmail = null)
    {
        return Enumerable.Range(0, count).Select(i => EventContactShareConsent.Grant(
            _tenantId, ContactShareConsentSubjectTypeEnum.User, Guid.CreateVersion7(), _actorId,
            ConsentPurposeCodes.OrganizerFutureCommunications,
            i == 0 && firstEmail is not null ? firstEmail : $"user{i}@example.com",
            "consent text", "v1", DateTime.UtcNow.AddDays(-i))).ToList();
    }

    #endregion
}
