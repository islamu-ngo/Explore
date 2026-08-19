// ABOUTME: Unit tests for reporter-owned event-report communication-consent updates.
// ABOUTME: Verifies owner, tenant, idempotency, audit-time, transaction, and cache behavior.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventReporting;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventReporting;
using Explore.Application.Features.EventReporting.Handlers.Commands;
using Explore.Application.Features.EventReporting.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventReporting.Commands;

public sealed class UpdateMyReportCommunicationConsentCommandHandlerTests
{
    private static readonly DateTime ChangedAt = new(2026, 7, 19, 20, 0, 0, DateTimeKind.Utc);

    private readonly IEventReportRepository _eventReportRepository = Substitute.For<IEventReportRepository>();
    private readonly IPrivacyErasureStateRepository _privacyErasureStateRepository = Substitute.For<IPrivacyErasureStateRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IAuthorizationProvider _authorizationProvider = Substitute.For<IAuthorizationProvider>();
    private readonly RecordingHybridCache _cache = new();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();

    public UpdateMyReportCommunicationConsentCommandHandlerTests()
    {
        _unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call
                .Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>()
                .Invoke(CancellationToken.None));
        _eventReportRepository.Update(Arg.Any<EventReport>()).Returns(Task.CompletedTask);
        _privacyErasureStateRepository.GetBySubjectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PrivacyErasureSaga?)null);
        _authorizationProvider.AuthorizeAsync(
                Arg.Is<AuthorizationRequest>(request =>
                    request != null &&
                    request.ResourceKind == ResourceKinds.User &&
                    request.Action == AuthorizationActions.Users.Update &&
                    request.Facts == null),
                Arg.Any<CancellationToken>())
            .Returns(AuthorizationDecision.Allow(AuthorizationProviderMetadata.Runtime));
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(ChangedAt));
    }

    [Test]
    public async Task Handle_WhenOwnerChangesConsent_PersistsBothPurposesAndEvictsDetailCache()
    {
        var tenantId = Guid.CreateVersion7();
        var reporterUserId = Guid.CreateVersion7();
        var report = CreateReport(tenantId, reporterUserId, caseUpdates: false, followUp: true);
        ConfigureIdentity(tenantId, reporterUserId);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>())
            .Returns(report);

        var result = await CreateHandler().Handle(
            CreateCommand(report.Id, caseUpdates: true, followUp: false),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(report.ReportCaseUpdatesConsent).IsTrue();
        await Assert.That(report.ReportFollowUpContactConsent).IsFalse();
        await Assert.That(report.UpdatedAt).IsEqualTo(ChangedAt);
        await _eventReportRepository.Received(1).Update(report);
        await Assert.That(_cache.RemovedKeys).Contains(
            $"event-reporting:my-report:{tenantId:N}:{reporterUserId:N}:{report.Id:N}");
    }

    [Test]
    public async Task Handle_WhenConsentIsUnchanged_DoesNotRotateAuditOrPersist()
    {
        var tenantId = Guid.CreateVersion7();
        var reporterUserId = Guid.CreateVersion7();
        var report = CreateReport(tenantId, reporterUserId, caseUpdates: true, followUp: false);
        var concurrencyStamp = report.ConcurrencyStamp;
        ConfigureIdentity(tenantId, reporterUserId);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>())
            .Returns(report);

        var result = await CreateHandler().Handle(
            CreateCommand(report.Id, caseUpdates: true, followUp: false),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(report.UpdatedAt).IsNull();
        await Assert.That(report.ConcurrencyStamp).IsEqualTo(concurrencyStamp);
        await _eventReportRepository.DidNotReceive().Update(Arg.Any<EventReport>());
        await Assert.That(_cache.RemovedKeys).IsEmpty();
    }

    [Test]
    public async Task Handle_WhenReportBelongsToAnotherUser_FailsClosed()
    {
        var tenantId = Guid.CreateVersion7();
        var currentUserId = Guid.CreateVersion7();
        var report = CreateReport(tenantId, Guid.CreateVersion7(), caseUpdates: false, followUp: false);
        ConfigureIdentity(tenantId, currentUserId);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>())
            .Returns(report);

        var result = await CreateHandler().Handle(
            CreateCommand(report.Id, caseUpdates: true, followUp: true),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.ReportNotFound);
        await _eventReportRepository.DidNotReceive().Update(Arg.Any<EventReport>());
    }

    [Test]
    public async Task Handle_WhenRepositoryReturnsAnotherTenant_FailsClosed()
    {
        var tenantId = Guid.CreateVersion7();
        var reporterUserId = Guid.CreateVersion7();
        var report = CreateReport(Guid.CreateVersion7(), reporterUserId, caseUpdates: false, followUp: false);
        ConfigureIdentity(tenantId, reporterUserId);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>())
            .Returns(report);

        var result = await CreateHandler().Handle(
            CreateCommand(report.Id, caseUpdates: true, followUp: true),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.ReportNotFound);
        await _eventReportRepository.DidNotReceive().Update(Arg.Any<EventReport>());
    }

    [Test]
    public async Task Handle_WhenReportIsMissing_FailsClosed()
    {
        var tenantId = Guid.CreateVersion7();
        var reporterUserId = Guid.CreateVersion7();
        var reportId = Guid.CreateVersion7();
        ConfigureIdentity(tenantId, reporterUserId);
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, reportId, Arg.Any<CancellationToken>())
            .Returns((EventReport?)null);

        var result = await CreateHandler().Handle(
            CreateCommand(reportId, caseUpdates: true, followUp: true),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.ReportNotFound);
    }

    [Test]
    public async Task Handle_WhenReporterIdentityIsMissing_FailsBeforeTransaction()
    {
        _tenantContext.TenantId.Returns(Guid.CreateVersion7());
        _currentUserService.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            CreateCommand(Guid.CreateVersion7(), caseUpdates: true, followUp: true),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventReportFailureCodes.UserUnresolved);
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
            Arg.Any<CancellationToken>());
        await _authorizationProvider.DidNotReceiveWithAnyArgs().AuthorizeAsync(default!, default);
    }

    [Test]
    public async Task Handle_WhenAuthorizationProviderDenies_FailsBeforeTransactionAndMutation()
    {
        var tenantId = Guid.CreateVersion7();
        var reporterUserId = Guid.CreateVersion7();
        var reportId = Guid.CreateVersion7();
        ConfigureIdentity(tenantId, reporterUserId);
        _authorizationProvider.AuthorizeAsync(
                Arg.Is<AuthorizationRequest>(request =>
                    request != null &&
                    request.ResourceKind == ResourceKinds.User &&
                    request.ResourceId == reporterUserId.ToString() &&
                    request.Action == AuthorizationActions.Users.Update &&
                    request.Facts == null),
                Arg.Any<CancellationToken>())
            .Returns(AuthorizationDecision.Deny(AuthorizationProviderMetadata.Runtime));

        await Assert.ThrowsAsync<AuthorizationException>(() => CreateHandler().Handle(
            CreateCommand(reportId, caseUpdates: true, followUp: true),
            CancellationToken.None));

        await _authorizationProvider.Received(1).AuthorizeAsync(
            Arg.Is<AuthorizationRequest>(request =>
                request != null &&
                request.ResourceKind == ResourceKinds.User &&
                request.ResourceId == reporterUserId.ToString() &&
                request.Action == AuthorizationActions.Users.Update &&
                request.Facts == null),
            Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
            Arg.Any<CancellationToken>());
        await _eventReportRepository.DidNotReceiveWithAnyArgs().GetByIdForUpdateAsync(
            default,
            default,
            default);
        await _eventReportRepository.DidNotReceive().Update(Arg.Any<EventReport>());
        await Assert.That(_cache.RemovedKeys).IsEmpty();
    }


    [Test]
    public async Task Handle_WhenFenceAlreadyExists_ReturnsMaskedFailureWithoutTransactionOrCacheRemoval()
    {
        var tenantId = Guid.CreateVersion7();
        var reporterUserId = Guid.CreateVersion7();
        var report = CreateReport(tenantId, reporterUserId, caseUpdates: false, followUp: true);
        ConfigureIdentity(tenantId, reporterUserId);
        _privacyErasureStateRepository.GetBySubjectAsync(reporterUserId, Arg.Any<CancellationToken>())
            .Returns(CreateFencedSaga(reporterUserId));

        var result = await CreateHandler().Handle(
            CreateCommand(report.Id, caseUpdates: true, followUp: false),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("privacy_erasure_fenced");
        await Assert.That(result.Errors).IsNull();
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
            Arg.Any<CancellationToken>());
        await _eventReportRepository.DidNotReceive().Update(Arg.Any<EventReport>());
        await Assert.That(_cache.RemovedKeys).IsEmpty();
    }

    [Test]
    public async Task Handle_WhenFenceAppearsDuringTransactionMasksDetailedErrorsAndSkipsMutation()
    {
        var tenantId = Guid.CreateVersion7();
        var reporterUserId = Guid.CreateVersion7();
        var report = CreateReport(tenantId, reporterUserId, caseUpdates: false, followUp: true);
        ConfigureIdentity(tenantId, reporterUserId);
        _privacyErasureStateRepository.GetBySubjectAsync(reporterUserId, Arg.Any<CancellationToken>())
            .Returns((PrivacyErasureSaga?)null, CreateFencedSaga(reporterUserId));
        _eventReportRepository.GetByIdForUpdateAsync(tenantId, report.Id, Arg.Any<CancellationToken>())
            .Returns(report);

        var result = await CreateHandler().Handle(
            CreateCommand(report.Id, caseUpdates: true, followUp: false),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("privacy_erasure_fenced");
        await Assert.That(result.Errors).IsNull();
        await _eventReportRepository.DidNotReceive().Update(Arg.Any<EventReport>());
        await Assert.That(_cache.RemovedKeys).IsEmpty();
    }

    private void ConfigureIdentity(Guid tenantId, Guid reporterUserId)
    {
        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(reporterUserId);
    }

    private UpdateMyReportCommunicationConsentCommandHandler CreateHandler() => new(
        _eventReportRepository,
        _privacyErasureStateRepository,
        _unitOfWork,
        _tenantContext,
        _currentUserService,
        _authorizationProvider,
        _cache,
        _timeProvider);

    private static PrivacyErasureSaga CreateFencedSaga(Guid userId)
    {
        DateTime nowUtc = DateTime.UtcNow;
        PrivacyErasureIntent intent = PrivacyErasureIntent.Record(
            Guid.CreateVersion7(),
            1,
            PrivacyErasureSubjectKind.User,
            userId,
            PrivacyErasureReasonCode.AccountDeletion,
            1,
            nowUtc,
            nowUtc);
        return PrivacyErasureSaga.Start(intent, 1, new byte[32], nowUtc.AddMinutes(5), nowUtc);
    }

    private static UpdateMyReportCommunicationConsentCommand CreateCommand(
        Guid reportId,
        bool caseUpdates,
        bool followUp) => new()
        {
            ReportId = reportId,
            Request = new UpdateMyReportCommunicationConsentDto
            {
                Consent = new ReportCommunicationConsentUpdateDto
                {
                    ReportCaseUpdatesConsent = caseUpdates,
                    ReportFollowUpContactConsent = followUp
                }
            }
        };

    private static EventReport CreateReport(
        Guid tenantId,
        Guid reporterUserId,
        bool caseUpdates,
        bool followUp)
    {
        return EventReport.Create(
            tenantId,
            Guid.CreateVersion7(),
            reporterUserId,
            Guid.CreateVersion7(),
            EventReporterKind.AuthenticatedUser,
            EventReportSourceKind.UserReport,
            "spam",
            subcategoryCode: null,
            EventReportPriority.Normal,
            severityHint: null,
            caseUpdates,
            followUp,
            reporterLocale: null,
            reporterIpHash: null,
            reporterUserAgentHash: null);
    }

    private sealed class RecordingHybridCache : HybridCache
    {
        public List<string> RemovedKeys { get; } = [];

        public override ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) => factory(state, cancellationToken);

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            RemovedKeys.Add(key);
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public override ValueTask SetAsync<T>(
            string key,
            T value,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
