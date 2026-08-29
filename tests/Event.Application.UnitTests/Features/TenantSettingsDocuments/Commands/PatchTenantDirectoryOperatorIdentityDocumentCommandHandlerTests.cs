// ABOUTME: Specifies tenant-bound presence-aware directory identity patches and optimistic concurrency.
// ABOUTME: Proves incomplete drafts are saveable while malformed, stale, and cross-tenant writes fail closed.

namespace Event.Application.UnitTests.Features.TenantSettingsDocuments.Commands;

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.TenantSettingsDocuments;
using Explore.Application.Exceptions;
using Explore.Application.Features.TenantSettingsDocuments.Handlers.Commands;
using Explore.Application.Features.TenantSettingsDocuments.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Settings.Documents;
using Explore.Domain.Settings.Documents.Payloads;
using NSubstitute;

public sealed class PatchTenantDirectoryOperatorIdentityDocumentCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService =
        Substitute.For<ICurrentUserService>();
    private readonly ITenantSettingsDocumentRepository _repository =
        Substitute.For<ITenantSettingsDocumentRepository>();
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly ISettingMutationLock _mutationLock = Substitute.For<ISettingMutationLock>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITypedSettingsDocumentResolver _resolver =
        Substitute.For<ITypedSettingsDocumentResolver>();

    public PatchTenantDirectoryOperatorIdentityDocumentCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUserService.UserId.Returns(_userId);
        _unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(call.ArgAt<CancellationToken>(1)));
        _mutationLock.ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<TenantDirectoryOperatorIdentityDocumentDto>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<BaseCommandResponse<TenantDirectoryOperatorIdentityDocumentDto>>>>()(
                call.ArgAt<CancellationToken>(2)));
    }

    [Test]
    public async Task Handle_MergesSpecifiedGroupAndPreservesIncompleteDraft()
    {
        TenantSettingsDocument document = DraftDocument();
        _tenantRepository.GetByIdAsNoTrackingAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(new Tenant
            {
                Id = _tenantId,
                FullName = "Provisioning tenant",
                Slug = "provisioning-tenant",
                TenantStatusId = (int)TenantStatusEnum.Provisioning,
                TenantStatus = null!
            });
        _repository.GetTrackedByTenantAndDocumentKey(
                _tenantId,
                SettingsDocumentKeys.Tenant.DirectoryOperatorIdentity,
                Arg.Any<CancellationToken>())
            .Returns(document);
        var handler = CreateHandler();

        BaseCommandResponse<TenantDirectoryOperatorIdentityDocumentDto> result =
            await handler.Handle(
                new PatchTenantDirectoryOperatorIdentityDocumentCommand
                {
                    TenantId = _tenantId,
                    Patch = new PatchTenantDirectoryOperatorIdentityDocumentDto
                    {
                        ExpectedConcurrencyStamp = document.ConcurrencyStamp,
                        LegalEntity = new PatchTenantDirectoryOperatorLegalEntityDto
                        {
                            LegalName = OptionalUpdate<string?>.Set(
                                " Community Events ASBL ")
                        }
                    }
                },
                CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id.Payload.PublicName).IsEqualTo("Community Events");
        await Assert.That(result.Id.Payload.LegalName).IsEqualTo("Community Events ASBL");
        await Assert.That(result.Id.IsActivationReady).IsFalse();
        TenantDirectoryOperatorIdentitySettings? persisted =
            JsonSerializer.Deserialize<TenantDirectoryOperatorIdentitySettings>(
                document.PayloadJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await Assert.That(persisted!.LegalName).IsEqualTo("Community Events ASBL");
        await Assert.That(document.UpdatedBy).IsEqualTo(_userId);
        await _repository.Received(1).Update(document);
        _resolver.Received(1).InvalidateTenantDocumentCache(
            _tenantId,
            SettingsDocumentKeys.Tenant.DirectoryOperatorIdentity);
    }

    [Test]
    public async Task Handle_StaleRevisionThrowsBeforeMutation()
    {
        TenantSettingsDocument document = DraftDocument();
        _repository.GetTrackedByTenantAndDocumentKey(
                _tenantId,
                SettingsDocumentKeys.Tenant.DirectoryOperatorIdentity,
                Arg.Any<CancellationToken>())
            .Returns(document);
        var handler = CreateHandler();

        await Assert.That(() => handler.Handle(
                new PatchTenantDirectoryOperatorIdentityDocumentCommand
                {
                    TenantId = _tenantId,
                    Patch = new PatchTenantDirectoryOperatorIdentityDocumentDto
                    {
                        ExpectedConcurrencyStamp = Guid.CreateVersion7(),
                        Contacts = new PatchTenantDirectoryOperatorContactsDto
                        {
                            PublicContactEmail =
                                OptionalUpdate<string?>.Set("contact@example.test")
                        }
                    }
                },
                CancellationToken.None))
            .Throws<ConcurrencyConflictException>();
        await _repository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Test]
    public async Task Handle_CrossTenantRequestFailsBeforeRepositoryRead()
    {
        var handler = CreateHandler();

        BaseCommandResponse<TenantDirectoryOperatorIdentityDocumentDto> result =
            await handler.Handle(
                new PatchTenantDirectoryOperatorIdentityDocumentCommand
                {
                    TenantId = Guid.CreateVersion7(),
                    Patch = new PatchTenantDirectoryOperatorIdentityDocumentDto
                    {
                        ExpectedConcurrencyStamp = Guid.CreateVersion7(),
                        Contacts = new PatchTenantDirectoryOperatorContactsDto
                        {
                            PublicContactEmail =
                                OptionalUpdate<string?>.Set("contact@example.test")
                        }
                    }
                },
                CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await _repository.DidNotReceiveWithAnyArgs()
            .GetTrackedByTenantAndDocumentKey(default, default!, default);
    }

    [Test]
    public async Task Handle_ActiveTenantRejectsPatchThatBreaksActivationReadinessWithoutRevisionLoss()
    {
        TenantSettingsDocument document = ActiveReadyDocument();
        _repository.GetTrackedByTenantAndDocumentKey(
                _tenantId,
                SettingsDocumentKeys.Tenant.DirectoryOperatorIdentity,
                Arg.Any<CancellationToken>())
            .Returns(document);
        _tenantRepository.GetByIdAsNoTrackingAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(new Tenant
            {
                Id = _tenantId,
                FullName = "Active tenant",
                Slug = "active-tenant",
                TenantStatusId = (int)TenantStatusEnum.Active,
                TenantStatus = null!
            });
        Tenant? loadedTenant = await _tenantRepository.GetByIdAsNoTrackingAsync(
            _tenantId,
            CancellationToken.None);
        await Assert.That(loadedTenant!.TenantStatusId).IsEqualTo((int)TenantStatusEnum.Active);
        Guid originalRevision = document.ConcurrencyStamp;
        string originalPayload = document.PayloadJson;

        BaseCommandResponse<TenantDirectoryOperatorIdentityDocumentDto> result =
            await CreateHandler().Handle(
                new PatchTenantDirectoryOperatorIdentityDocumentCommand
                {
                    TenantId = _tenantId,
                    Patch = new PatchTenantDirectoryOperatorIdentityDocumentDto
                    {
                        ExpectedConcurrencyStamp = originalRevision,
                        LegalEntity = new PatchTenantDirectoryOperatorLegalEntityDto
                        {
                            LegalName = OptionalUpdate<string?>.Set(null)
                        }
                    }
                },
                CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();

        await Assert.That(document.ConcurrencyStamp).IsEqualTo(originalRevision);
        await Assert.That(document.PayloadJson).IsEqualTo(originalPayload);
        await _repository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    private PatchTenantDirectoryOperatorIdentityDocumentCommandHandler CreateHandler() =>
        new(
            _tenantContext,
            _currentUserService,
            _repository,
            _tenantRepository,
            _mutationLock,
            _resolver);

    private TenantSettingsDocument ActiveReadyDocument()
    {
        TenantSettingsDocument document = TenantDirectoryOperatorIdentityDocumentDefaults.Create(
            _tenantId,
            new TenantDirectoryOperatorIdentitySettings
            {
                PublicName = "Community Events",
                LegalName = "Community Events ASBL",
                OperatorKindCode = "registered_organization",
                JurisdictionCountryCode = "BE",
                PublicContactEmail = "contact.test",
                LegalNoticeUrl = "https://example.test/legal",
                PrivacyUrl = "https://example.test/privacy"
            });
        document.Id = Guid.CreateVersion7();
        document.ConcurrencyStamp = Guid.CreateVersion7();
        return document;
    }

    private TenantSettingsDocument DraftDocument()
    {
        TenantSettingsDocument document =
            TenantDirectoryOperatorIdentityDocumentDefaults.Create(
                _tenantId,
                "Community Events");
        document.Id = Guid.CreateVersion7();
        document.ConcurrencyStamp = Guid.CreateVersion7();
        return document;
    }
}
