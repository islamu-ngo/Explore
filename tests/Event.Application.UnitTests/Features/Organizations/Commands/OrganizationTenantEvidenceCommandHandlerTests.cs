// ABOUTME: Unit tests for OrganizationTenant legitimacy-evidence submission and tenant-local review.
// ABOUTME: Verifies exact storage ownership, separate reviewer authority, and no automatic participation approval.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.OrganizationTenantEvidence;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Features.OrganizationTenantEvidence.Handlers.Commands;
using Explore.Application.Features.OrganizationTenantEvidence.Requests.Commands;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Organizations.Commands;

public sealed class OrganizationTenantEvidenceCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _organizationId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly IOrganizationTenantRepository _organizationTenantRepository =
        Substitute.For<IOrganizationTenantRepository>();
    private readonly IOrganizationTenantEvidenceRepository _evidenceRepository =
        Substitute.For<IOrganizationTenantEvidenceRepository>();
    private readonly IStorageObjectRepository _storageObjectRepository =
        Substitute.For<IStorageObjectRepository>();
    private readonly IAdminContext _adminContext = Substitute.For<IAdminContext>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ISender _sender = Substitute.For<ISender>();

    public OrganizationTenantEvidenceCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUserService.UserId.Returns(_userId);
        _unitOfWork
            .ExecuteSerializableAsync(
                Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>()(CancellationToken.None));
        _unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>()(CancellationToken.None));
    }

    [Test]
    public async Task CreateUploadSession_BindsPrivateDocumentToPendingParticipation()
    {
        var participation = CreateParticipation();
        _adminContext.IsOrganizationAdminAsync(_organizationId, Arg.Any<CancellationToken>()).Returns(true);
        _organizationTenantRepository
            .GetByOrganizationAndTenant(_organizationId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(participation);
        _sender.Send(
                Arg.Any<CreateStorageUploadSessionCommand>(),
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<StorageUploadSessionDto>
            {
                Success = true,
                Id = new StorageUploadSessionDto
                {
                    Provider = StorageProviders.Local,
                    ContentType = "application/pdf",
                    SafeDisplayName = "evidence.pdf",
                    Purpose = StorageObjectPurposes.Document,
                    Visibility = StorageObjectVisibilities.PrivateOwner,
                    Status = StorageUploadSessionStates.Reserved
                }
            });

        var result = await new CreateOrganizationTenantEvidenceUploadSessionCommandHandler(
            _organizationTenantRepository,
            _adminContext,
            _tenantContext,
            _sender).Handle(
            new CreateOrganizationTenantEvidenceUploadSessionCommand
            {
                OrganizationId = _organizationId,
                Upload = new CreateOrganizationTenantEvidenceUploadSessionDto
                {
                    FileName = "evidence.pdf",
                    ContentType = "application/pdf",
                    ExpectedSizeBytes = 32
                }
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _sender.Received(1).Send(
            Arg.Is<CreateStorageUploadSessionCommand>(command =>
                command.TenantId == _tenantId
                && command.UploadSessionDto.Purpose == StorageObjectPurposes.Document
                && command.UploadSessionDto.Visibility == StorageObjectVisibilities.PrivateOwner
                && command.UploadSessionDto.OwningResourceKind == StorageOwningResourceKinds.OrganizationTenant
                && command.UploadSessionDto.OwningResourceId == participation.Id),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Submit_WithDocumentOwnedByAnotherParticipation_FailsClosed()
    {
        var participation = CreateParticipation();
        var document = CreateDocument(Guid.CreateVersion7());
        _adminContext.IsOrganizationAdminAsync(_organizationId, Arg.Any<CancellationToken>()).Returns(true);
        _organizationTenantRepository
            .GetByOrganizationAndTenant(_organizationId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(participation);
        _storageObjectRepository
            .GetEvidenceDocumentAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await CreateSubmitHandler().Handle(
            new SubmitOrganizationTenantEvidenceCommand
            {
                OrganizationId = _organizationId,
                Evidence = new SubmitOrganizationTenantEvidenceDto
                {
                    DocumentStorageObjectId = document.Id
                }
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _evidenceRepository.DidNotReceive().Create(Arg.Any<OrganizationTenantEvidence>());
    }

    [Test]
    public async Task Submit_WithEligibleDocument_AttachesPendingEvidenceWithoutApprovingParticipation()
    {
        var participation = CreateParticipation();
        var document = CreateDocument(participation.Id);
        _adminContext.IsOrganizationAdminAsync(_organizationId, Arg.Any<CancellationToken>()).Returns(true);
        _organizationTenantRepository
            .GetByOrganizationAndTenant(_organizationId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(participation);
        _storageObjectRepository
            .GetEvidenceDocumentAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);
        _evidenceRepository
            .GetByDocumentAsync(participation.Id, document.Id, Arg.Any<CancellationToken>())
            .Returns((OrganizationTenantEvidence?)null);
        _evidenceRepository.Create(Arg.Any<OrganizationTenantEvidence>())
            .Returns(call => call.Arg<OrganizationTenantEvidence>());

        var result = await CreateSubmitHandler().Handle(
            new SubmitOrganizationTenantEvidenceCommand
            {
                OrganizationId = _organizationId,
                Evidence = new SubmitOrganizationTenantEvidenceDto
                {
                    DocumentStorageObjectId = document.Id
                }
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(participation.ApprovalStatusId).IsEqualTo((int)ApprovalStatusEnum.Pending);
        await _evidenceRepository.Received(1).Create(Arg.Is<OrganizationTenantEvidence>(evidence =>
            evidence.TenantId == _tenantId
            && evidence.OrganizationTenantId == participation.Id
            && evidence.DocumentStorageObjectId == document.Id
            && evidence.ReviewStatusId == (int)ApprovalStatusEnum.Pending));
    }

    [Test]
    public async Task Review_AsOrganizationAdminWithoutTenantAuthority_IsRejected()
    {
        _adminContext.IsTenantAdminAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateReviewHandler().Handle(
            CreateReviewCommand(Guid.CreateVersion7(), Guid.CreateVersion7()),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _evidenceRepository.DidNotReceive()
            .GetDetailsAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Review_AsTenantAdmin_RecordsDecisionWithoutApprovingParticipation()
    {
        var participation = CreateParticipation();
        var document = CreateDocument(participation.Id);
        var evidence = OrganizationTenantEvidence.CreatePending(participation, document);
        evidence.ConcurrencyStamp = Guid.CreateVersion7();
        _adminContext.IsTenantAdminAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(true);
        _evidenceRepository
            .GetDetailsAsync(evidence.Id, trackChanges: true, Arg.Any<CancellationToken>())
            .Returns(evidence);

        var result = await CreateReviewHandler().Handle(
            CreateReviewCommand(evidence.Id, evidence.ConcurrencyStamp),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(evidence.ReviewStatusId).IsEqualTo((int)ApprovalStatusEnum.Approved);
        await Assert.That(evidence.ReviewedByUserId).IsEqualTo(_userId);
        await Assert.That(participation.ApprovalStatusId).IsEqualTo((int)ApprovalStatusEnum.Pending);
        await _evidenceRepository.Received(1).Update(evidence);
    }

    private SubmitOrganizationTenantEvidenceCommandHandler CreateSubmitHandler() => new(
        _organizationTenantRepository,
        _evidenceRepository,
        _storageObjectRepository,
        _adminContext,
        _tenantContext,
        _unitOfWork);

    private ReviewOrganizationTenantEvidenceCommandHandler CreateReviewHandler() => new(
        _evidenceRepository,
        _adminContext,
        _tenantContext,
        _currentUserService,
        _unitOfWork);

    private ReviewOrganizationTenantEvidenceCommand CreateReviewCommand(
        Guid evidenceId,
        Guid concurrencyStamp) => new()
        {
            OrganizationId = _organizationId,
            EvidenceId = evidenceId,
            Review = new ReviewOrganizationTenantEvidenceDto
            {
                Decision = OrganizationTenantEvidenceReviewDecisionDto.Approve,
                ExpectedConcurrencyStamp = concurrencyStamp,
                Notes = "verified"
            }
        };

    private OrganizationTenant CreateParticipation()
    {
        return new OrganizationTenant
        {
            Id = Guid.CreateVersion7(),
            TenantId = _tenantId,
            Tenant = null!,
            OrganizationId = _organizationId,
            Organization = new Organization
            {
                Id = _organizationId,
                Pii = new OrganizationPii
                {
                    OrganizationId = _organizationId,
                    FullName = "Organization",
                    Email = "org@example.test",
                    Country = "BE",
                    City = "Brussels",
                    Postcode = "1000",
                    Address = "Street"
                }
            },
            ApprovalStatusId = (int)ApprovalStatusEnum.Pending,
            ApprovalStatus = null!
        };
    }

    private StorageObject CreateDocument(Guid owningParticipationId)
    {
        return new StorageObject
        {
            Id = Guid.CreateVersion7(),
            TenantId = _tenantId,
            Tenant = null!,
            FileTypeId = (int)FileTypeEnum.Document,
            FileType = null!,
            Uri = "/api/storageobject/document/content",
            ObjectKey = $"tenants/{_tenantId:N}/documents/evidence.pdf",
            Provider = StorageProviders.Local,
            FullName = "evidence.pdf",
            SafeDisplayName = "evidence.pdf",
            Extension = "pdf",
            ContentType = "application/pdf",
            Size = 32,
            Visibility = StorageObjectVisibilities.PrivateOwner,
            Purpose = StorageObjectPurposes.Document,
            LifecycleState = StorageObjectLifecycleStates.Active,
            OwningResourceKind = StorageOwningResourceKinds.OrganizationTenant,
            OwningResourceId = owningParticipationId
        };
    }
}
