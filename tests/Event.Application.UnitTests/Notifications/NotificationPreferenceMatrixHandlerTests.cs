// ABOUTME: Unit tests for privacy-fenced notification preference matrix handlers.
// ABOUTME: Covers atomic user writes, projections, required cells, and mute lock behavior.

namespace Event.Application.UnitTests.Notifications;

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Notification;
using Explore.Application.Features.Notifications.Handlers.Commands;
using Explore.Application.Features.Notifications.Handlers.Queries;
using Explore.Application.Features.Notifications.Requests.Commands;
using Explore.Application.Features.Notifications.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

public sealed class NotificationPreferenceMatrixHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("018f0000-0000-7000-8000-000000000001");
    private static readonly Guid UserId = Guid.Parse("018f0000-0000-7000-8000-000000000002");

    private readonly INotificationChannelPreferenceRepository _preferenceRepository = Substitute.For<INotificationChannelPreferenceRepository>();
    private readonly INotificationPreferenceProfileRepository _profileRepository = Substitute.For<INotificationPreferenceProfileRepository>();
    private readonly INotificationPreferenceResolver _resolver = Substitute.For<INotificationPreferenceResolver>();
    private readonly IGroupTenantRepository _groupTenantRepository = Substitute.For<IGroupTenantRepository>();
    private readonly IOrganizationTenantRepository _organizationTenantRepository = Substitute.For<IOrganizationTenantRepository>();
    private readonly IPrivacyErasureStateRepository _privacyErasureStateRepository = Substitute.For<IPrivacyErasureStateRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly RecordingUnitOfWork _unitOfWork = new();

    public NotificationPreferenceMatrixHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _currentUserService.UserId.Returns(UserId);
        _currentUserService.IsAuthenticated.Returns(true);
    }

    [Test]
    public async Task Query_ReturnsUiReadyMatrixFromMetadataAndResolverDecisions()
    {
        SetupMetadata();
        _resolver.ResolveBatchAsync(Arg.Any<IReadOnlyCollection<NotificationPreferenceResolveRequest>>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<IReadOnlyList<NotificationPreferenceDecision>>(
                ((IReadOnlyCollection<NotificationPreferenceResolveRequest>)call[0]!)
                .Select(request => new NotificationPreferenceDecision(
                    request.CategoryCode,
                    request.ChannelCode,
                    IsEnabled: request.CategoryCode == NotificationPreferenceCategoryCodes.AccountSecurity,
                    IsRequired: request.CategoryCode == NotificationPreferenceCategoryCodes.AccountSecurity,
                    IsLocked: request.CategoryCode == NotificationPreferenceCategoryCodes.AccountSecurity,
                    IsMuted: false,
                    EffectiveSourceScope: request.CategoryCode == NotificationPreferenceCategoryCodes.AccountSecurity ? "RequiredCategory" : "Default",
                    LockReason: request.CategoryCode == NotificationPreferenceCategoryCodes.AccountSecurity ? "Required notification category" : null))
                .ToArray()));
        var handler = new GetCurrentUserNotificationPreferenceMatrixQueryHandler(
            _preferenceRepository,
            _resolver,
            _tenantContext,
            _currentUserService);

        var result = await handler.Handle(new GetCurrentUserNotificationPreferenceMatrixQuery(), CancellationToken.None);

        await Assert.That(result.TenantId).IsEqualTo(TenantId);
        await Assert.That(result.UserId).IsEqualTo(UserId);
        await Assert.That(result.Categories.Count).IsEqualTo(2);
        await Assert.That(result.Channels.Count).IsEqualTo(2);
        await Assert.That(result.Cells.Count).IsEqualTo(4);
        var requiredCell = result.Cells.Single(cell => cell.CategoryCode == NotificationPreferenceCategoryCodes.AccountSecurity
            && cell.ChannelCode == NotificationPreferenceChannelCodes.Email);
        await Assert.That(requiredCell.IsEnabled).IsTrue();
        await Assert.That(requiredCell.IsEditable).IsFalse();
        await Assert.That(requiredCell.EffectiveSourceScope).IsEqualTo("RequiredCategory");
    }

    [Test]
    public async Task Save_DisablingRequiredCategoryFailsBeforeWrite()
    {
        SetupMetadata();
        var handler = new UpdateCurrentUserNotificationPreferenceMatrixCommandHandler(
            _preferenceRepository,
            _resolver,
            _unitOfWork,
            _privacyErasureStateRepository,
            _tenantContext,
            _currentUserService);

        var result = await handler.Handle(new UpdateCurrentUserNotificationPreferenceMatrixCommand
        {
            Cells = [new UpdateNotificationPreferenceCellDto
            {
                CategoryCode = NotificationPreferenceCategoryCodes.AccountSecurity,
                ChannelCode = NotificationPreferenceChannelCodes.Email,
                IsEnabled = false
            }]
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors!).Contains("Category 'account-security' is required and cannot be disabled.");
        await Assert.That(_unitOfWork.ExecuteCount).IsEqualTo(1);
        await _preferenceRepository.DidNotReceive().UpsertUserPreferenceAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Save_EditableCellWritesInsideTransaction()
    {
        SetupMetadata();
        _resolver.ResolveAsync(Arg.Any<NotificationPreferenceResolveRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(new NotificationPreferenceDecision(
                ((NotificationPreferenceResolveRequest)call[0]!).CategoryCode,
                ((NotificationPreferenceResolveRequest)call[0]!).ChannelCode,
                IsEnabled: false,
                IsRequired: false,
                IsLocked: false,
                IsMuted: false,
                EffectiveSourceScope: "Default",
                LockReason: null)));
        _preferenceRepository.UpsertUserPreferenceAsync(
                TenantId,
                UserId,
                (int)NotificationPreferenceCategoryEnum.Marketing,
                (int)NotificationPreferenceChannelEnum.Email,
                true,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new NotificationChannelPreference
            {
                Id = Guid.Parse("018f0000-0000-7000-8000-000000000003"),
                TenantId = TenantId,
                Tenant = null!,
                ScopeId = (int)ConfigurationScopeEnum.User,
                Scope = null!,
                UserId = UserId,
                CategoryId = (int)NotificationPreferenceCategoryEnum.Marketing,
                Category = null!,
                ChannelId = (int)NotificationPreferenceChannelEnum.Email,
                Channel = null!,
                IsEnabled = true
            }));
        var handler = new UpdateCurrentUserNotificationPreferenceMatrixCommandHandler(
            _preferenceRepository,
            _resolver,
            _unitOfWork,
            _privacyErasureStateRepository,
            _tenantContext,
            _currentUserService);

        var result = await handler.Handle(new UpdateCurrentUserNotificationPreferenceMatrixCommand
        {
            Cells = [new UpdateNotificationPreferenceCellDto
            {
                CategoryCode = NotificationPreferenceCategoryCodes.Marketing,
                ChannelCode = NotificationPreferenceChannelCodes.Email,
                IsEnabled = true
            }]
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(_unitOfWork.ExecuteCount).IsEqualTo(1);
        await _preferenceRepository.Received(1).UpsertUserPreferenceAsync(
            TenantId,
            UserId,
            (int)NotificationPreferenceCategoryEnum.Marketing,
            (int)NotificationPreferenceChannelEnum.Email,
            true,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Save_WhenFenceAppearsBeforeTransactionDoesNotWritePreferences()
    {
        SetupMetadata();
        _resolver.ResolveAsync(Arg.Any<NotificationPreferenceResolveRequest>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationPreferenceDecision(
                NotificationPreferenceCategoryCodes.Marketing,
                NotificationPreferenceChannelCodes.Email,
                IsEnabled: false,
                IsRequired: false,
                IsLocked: false,
                IsMuted: false,
                EffectiveSourceScope: "Default",
                LockReason: null));
        _privacyErasureStateRepository
            .GetBySubjectAsync(UserId, Arg.Any<CancellationToken>())
            .Returns((PrivacyErasureSaga?)null, CreatePrivacyErasureSaga());
        var handler = new UpdateCurrentUserNotificationPreferenceMatrixCommandHandler(
            _preferenceRepository,
            _resolver,
            _unitOfWork,
            _privacyErasureStateRepository,
            _tenantContext,
            _currentUserService);

        var result = await handler.Handle(new UpdateCurrentUserNotificationPreferenceMatrixCommand
        {
            Cells = [new UpdateNotificationPreferenceCellDto
            {
                CategoryCode = NotificationPreferenceCategoryCodes.Marketing,
                ChannelCode = NotificationPreferenceChannelCodes.Email,
                IsEnabled = true
            }]
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Notification preference update failed.");
        await Assert.That(result.Errors).IsNull();
        await Assert.That(_unitOfWork.ExecuteCount).IsEqualTo(1);
        await _privacyErasureStateRepository.Received(2)
            .GetBySubjectAsync(UserId, Arg.Any<CancellationToken>());
        await _preferenceRepository.DidNotReceive().UpsertUserPreferenceAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Save_WhenFenceAppearsDuringValidationMasksDetailedErrors()
    {
        SetupMetadata();
        _privacyErasureStateRepository
            .GetBySubjectAsync(UserId, Arg.Any<CancellationToken>())
            .Returns((PrivacyErasureSaga?)null, CreatePrivacyErasureSaga());
        var handler = new UpdateCurrentUserNotificationPreferenceMatrixCommandHandler(
            _preferenceRepository,
            _resolver,
            _unitOfWork,
            _privacyErasureStateRepository,
            _tenantContext,
            _currentUserService);

        var result = await handler.Handle(new UpdateCurrentUserNotificationPreferenceMatrixCommand
        {
            Cells = [new UpdateNotificationPreferenceCellDto
            {
                CategoryCode = NotificationPreferenceCategoryCodes.AccountSecurity,
                ChannelCode = NotificationPreferenceChannelCodes.Email,
                IsEnabled = false
            }]
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Notification preference update failed.");
        await Assert.That(result.Errors).IsNull();
        await Assert.That(_unitOfWork.ExecuteCount).IsEqualTo(1);
        await _preferenceRepository.DidNotReceive().UpsertUserPreferenceAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Save_WhenSerializableDelegateRetriesReplaysEveryCellAndReturnsFinalResult()
    {
        SetupMetadata();
        _unitOfWork.RetryNextSerializableExecution = true;
        _resolver.ResolveAsync(Arg.Any<NotificationPreferenceResolveRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => new NotificationPreferenceDecision(
                call.ArgAt<NotificationPreferenceResolveRequest>(0).CategoryCode,
                call.ArgAt<NotificationPreferenceResolveRequest>(0).ChannelCode,
                IsEnabled: false,
                IsRequired: false,
                IsLocked: false,
                IsMuted: false,
                EffectiveSourceScope: "Default",
                LockReason: null));
        var emailId = Guid.Parse("018f0000-0000-7000-8000-000000000020");
        var inAppId = Guid.Parse("018f0000-0000-7000-8000-000000000021");
        _preferenceRepository.UpsertUserPreferenceAsync(
                TenantId,
                UserId,
                (int)NotificationPreferenceCategoryEnum.Marketing,
                Arg.Any<int>(),
                true,
                Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(new NotificationChannelPreference
            {
                Id = call.ArgAt<int>(3) == (int)NotificationPreferenceChannelEnum.Email ? emailId : inAppId,
                TenantId = TenantId,
                Tenant = null!,
                ScopeId = (int)ConfigurationScopeEnum.User,
                Scope = null!,
                UserId = UserId,
                CategoryId = (int)NotificationPreferenceCategoryEnum.Marketing,
                Category = null!,
                ChannelId = call.ArgAt<int>(3),
                Channel = null!,
                IsEnabled = true
            }));
        var handler = new UpdateCurrentUserNotificationPreferenceMatrixCommandHandler(
            _preferenceRepository,
            _resolver,
            _unitOfWork,
            _privacyErasureStateRepository,
            _tenantContext,
            _currentUserService);

        var result = await handler.Handle(new UpdateCurrentUserNotificationPreferenceMatrixCommand
        {
            Cells =
            [
                new UpdateNotificationPreferenceCellDto
                {
                    CategoryCode = NotificationPreferenceCategoryCodes.Marketing,
                    ChannelCode = NotificationPreferenceChannelCodes.Email,
                    IsEnabled = true
                },
                new UpdateNotificationPreferenceCellDto
                {
                    CategoryCode = NotificationPreferenceCategoryCodes.Marketing,
                    ChannelCode = NotificationPreferenceChannelCodes.InApp,
                    IsEnabled = true
                }
            ]
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(inAppId);
        await Assert.That(_unitOfWork.ExecuteCount).IsEqualTo(1);
        await Assert.That(_unitOfWork.SerializableAttemptCount).IsEqualTo(2);
        await _preferenceRepository.Received(2).UpsertUserPreferenceAsync(
            TenantId,
            UserId,
            (int)NotificationPreferenceCategoryEnum.Marketing,
            (int)NotificationPreferenceChannelEnum.Email,
            true,
            Arg.Any<CancellationToken>());
        await _preferenceRepository.Received(2).UpsertUserPreferenceAsync(
            TenantId,
            UserId,
            (int)NotificationPreferenceCategoryEnum.Marketing,
            (int)NotificationPreferenceChannelEnum.InApp,
            true,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GroupQuery_UsesParentOrganizationContextForResolution()
    {
        var groupId = Guid.Parse("018f0000-0000-7000-8000-000000000010");
        var organizationId = Guid.Parse("018f0000-0000-7000-8000-000000000011");
        SetupMetadata();
        Guid organizationTenantId = Guid.CreateVersion7();
        _groupTenantRepository.GetByGroupAndTenant(groupId, TenantId, Arg.Any<CancellationToken>()).Returns(new GroupTenant
        {
            Id = Guid.CreateVersion7(),
            GroupId = groupId,
            Group = new Group { Id = groupId, FullName = "Community group" },
            ApprovalStatus = null!,
            TenantId = TenantId,
            Tenant = null!,
            ParentOrganizationTenantId = organizationTenantId
        });
        _organizationTenantRepository.GetById(organizationTenantId).Returns(new OrganizationTenant
        {
            Id = organizationTenantId,
            OrganizationId = organizationId,
            Organization = new Organization { Id = organizationId, Pii = new OrganizationPii { FullName = "Organization" } },
            ApprovalStatus = null!,
            TenantId = TenantId,
            Tenant = null!
        });
        _resolver.ResolveBatchAsync(Arg.Any<IReadOnlyCollection<NotificationPreferenceResolveRequest>>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<IReadOnlyList<NotificationPreferenceDecision>>(
                ((IReadOnlyCollection<NotificationPreferenceResolveRequest>)call[0]!)
                .Select(request => new NotificationPreferenceDecision(
                    request.CategoryCode,
                    request.ChannelCode,
                    IsEnabled: false,
                    IsRequired: false,
                    IsLocked: false,
                    IsMuted: false,
                    EffectiveSourceScope: "Default",
                    LockReason: null))
                .ToArray()));
        var handler = new GetGroupNotificationPreferenceMatrixQueryHandler(
            _preferenceRepository,
            _resolver,
            _groupTenantRepository,
            _organizationTenantRepository,
            _tenantContext,
            _currentUserService);

        var result = await handler.Handle(new GetGroupNotificationPreferenceMatrixQuery { GroupId = groupId }, CancellationToken.None);

        await Assert.That(result.Scope).IsEqualTo("group");
        await Assert.That(result.GroupId).IsEqualTo(groupId);
        await Assert.That(result.OrganizationId).IsEqualTo(organizationId);
        await _resolver.Received(1).ResolveBatchAsync(
            Arg.Is<IReadOnlyCollection<NotificationPreferenceResolveRequest>>(requests =>
                requests.All(request => request.OrganizationId == organizationId && request.GroupId == groupId)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task OrganizationSave_WritesOrganizationScopedCellInsideTransaction()
    {
        var organizationId = Guid.Parse("018f0000-0000-7000-8000-000000000012");
        SetupMetadata();
        _resolver.ResolveAsync(Arg.Any<NotificationPreferenceResolveRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(new NotificationPreferenceDecision(
                ((NotificationPreferenceResolveRequest)call[0]!).CategoryCode,
                ((NotificationPreferenceResolveRequest)call[0]!).ChannelCode,
                IsEnabled: false,
                IsRequired: false,
                IsLocked: false,
                IsMuted: false,
                EffectiveSourceScope: "Default",
                LockReason: null)));
        _preferenceRepository.UpsertOrganizationPreferenceAsync(
                TenantId,
                organizationId,
                (int)NotificationPreferenceCategoryEnum.Marketing,
                (int)NotificationPreferenceChannelEnum.Email,
                true,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new NotificationChannelPreference
            {
                Id = Guid.Parse("018f0000-0000-7000-8000-000000000013"),
                TenantId = TenantId,
                Tenant = null!,
                ScopeId = (int)ConfigurationScopeEnum.Organization,
                Scope = null!,
                OrganizationId = organizationId,
                CategoryId = (int)NotificationPreferenceCategoryEnum.Marketing,
                Category = null!,
                ChannelId = (int)NotificationPreferenceChannelEnum.Email,
                Channel = null!,
                IsEnabled = true
            }));
        var handler = new UpdateOrganizationNotificationPreferenceMatrixCommandHandler(
            _preferenceRepository,
            _resolver,
            _unitOfWork,
            _tenantContext,
            _currentUserService);

        var result = await handler.Handle(new UpdateOrganizationNotificationPreferenceMatrixCommand
        {
            OrganizationId = organizationId,
            Cells = [new UpdateNotificationPreferenceCellDto
            {
                CategoryCode = NotificationPreferenceCategoryCodes.Marketing,
                ChannelCode = NotificationPreferenceChannelCodes.Email,
                IsEnabled = true
            }]
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(_unitOfWork.ExecuteCount).IsEqualTo(1);
        await _preferenceRepository.Received(1).UpsertOrganizationPreferenceAsync(
            TenantId,
            organizationId,
            (int)NotificationPreferenceCategoryEnum.Marketing,
            (int)NotificationPreferenceChannelEnum.Email,
            true,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SetMute_BroaderLockedProfileFailsBeforeWrite()
    {
        _profileRepository.ListForUserContextAsync(TenantId, UserId, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<NotificationPreferenceProfile>>([
                new NotificationPreferenceProfile
                {
                    Id = Guid.Parse("018f0000-0000-7000-8000-000000000004"),
                    TenantId = TenantId,
                    Tenant = null!,
                    ScopeId = (int)ConfigurationScopeEnum.Tenant,
                    Scope = null!,
                    IsLocked = true
                }
            ]));
        var handler = new SetCurrentUserNotificationPreferenceMuteCommandHandler(
            _profileRepository,
            _unitOfWork,
            _tenantContext,
            _currentUserService);

        var result = await handler.Handle(new SetCurrentUserNotificationPreferenceMuteCommand { IsMuted = true }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("Tenant scope");
        await Assert.That(_unitOfWork.ExecuteCount).IsEqualTo(0);
        await _profileRepository.DidNotReceive().UpsertUserMuteAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    private void SetupMetadata()
    {
        _preferenceRepository.ListCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<NotificationPreferenceCategory>>([
                new NotificationPreferenceCategory
                {
                    Id = (int)NotificationPreferenceCategoryEnum.AccountSecurity,
                    MasterCode = NotificationPreferenceCategoryCodes.AccountSecurity,
                    FullName = "Account security",
                    IsRequired = true,
                    DefaultEmailEnabled = true,
                    DefaultInAppEnabled = true,
                    SortOrder = 10
                },
                new NotificationPreferenceCategory
                {
                    Id = (int)NotificationPreferenceCategoryEnum.Marketing,
                    MasterCode = NotificationPreferenceCategoryCodes.Marketing,
                    FullName = "Marketing",
                    DefaultEmailEnabled = false,
                    DefaultInAppEnabled = false,
                    SortOrder = 90
                }
            ]));
        _preferenceRepository.ListChannelsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<NotificationPreferenceChannel>>([
                new NotificationPreferenceChannel
                {
                    Id = (int)NotificationPreferenceChannelEnum.Email,
                    MasterCode = NotificationPreferenceChannelCodes.Email,
                    FullName = "Email",
                    SortOrder = 10
                },
                new NotificationPreferenceChannel
                {
                    Id = (int)NotificationPreferenceChannelEnum.InApp,
                    MasterCode = NotificationPreferenceChannelCodes.InApp,
                    FullName = "In-App",
                    SortOrder = 20
                }
            ]));
    }

    private static PrivacyErasureSaga CreatePrivacyErasureSaga()
    {
        DateTime nowUtc = DateTime.UtcNow;
        PrivacyErasureIntent intent = PrivacyErasureIntent.Record(
            Guid.CreateVersion7(),
            1,
            PrivacyErasureSubjectKind.User,
            UserId,
            PrivacyErasureReasonCode.AccountDeletion,
            1,
            nowUtc,
            nowUtc);
        return PrivacyErasureSaga.Start(intent, 1, new byte[32], nowUtc.AddMinutes(5), nowUtc);
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int ExecuteCount { get; private set; }
        public int SerializableAttemptCount { get; private set; }
        public bool RetryNextSerializableExecution { get; set; }

        public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
        {
            ExecuteCount++;
            await operation(cancellationToken);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            ExecuteCount++;
            return await operation(cancellationToken);
        }

        public async Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            ExecuteCount++;
            SerializableAttemptCount++;
            if (RetryNextSerializableExecution)
            {
                RetryNextSerializableExecution = false;
                await operation(cancellationToken);
                SerializableAttemptCount++;
            }

            return await operation(cancellationToken);
        }
    }
}
