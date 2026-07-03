// ABOUTME: Unit tests for authenticated reporter-owned report status reads.
// ABOUTME: Verifies ownership scoping, limited projection shape, and user-scoped HybridCache keys.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventReporting.Handlers.Queries;
using Explore.Application.Features.EventReporting.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventReporting.Queries;

public sealed class GetMyReportRequestHandlerTests
{
    private readonly IEventReportRepository _eventReportRepository = Substitute.For<IEventReportRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly TestHybridCache _cache = new();

    [Test]
    public async Task Handle_WhenReportBelongsToCurrentUser_ReturnsLimitedReporterProjection()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var eventId = Guid.CreateVersion7();
        var createdAt = new DateTime(2026, 7, 2, 9, 30, 0, DateTimeKind.Utc);
        var updatedAt = createdAt.AddHours(1);
        var report = CreateReport(tenantId, eventId, userId, createdAt);
        report.UpdateStatus(EventReportStatus.UnderReview, updatedAt);
        report.EvidenceItems.Add(EventReportEvidence.CreateReporterText(
            tenantId,
            report.Id,
            "encrypted-sensitive-reporter-text",
            EventReportEvidenceClassification.Sensitive,
            createdAt.AddDays(30),
            userId,
            createdAt));

        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _eventReportRepository.GetByIdAsync(tenantId, report.Id, Arg.Any<CancellationToken>())
            .Returns(report);

        var result = await CreateHandler().Handle(
            new GetMyReportRequest { ReportId = report.Id },
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(report.Id);
        await Assert.That(result.EventId).IsEqualTo(eventId);
        await Assert.That(result.StatusId).IsEqualTo((int)EventReportStatus.UnderReview);
        await Assert.That(result.StatusCode).IsEqualTo("under_review");
        await Assert.That(result.StatusName).IsEqualTo(nameof(EventReportStatus.UnderReview));
        await Assert.That(result.ReasonCode).IsEqualTo("spam");
        await Assert.That(result.ReasonName).IsEqualTo("Spam");
        await Assert.That(result.SubcategoryCode).IsEqualTo("organizer");
        await Assert.That(result.SubmittedAtUtc).IsEqualTo(createdAt);
        await Assert.That(result.LastUpdatedAtUtc).IsEqualTo(updatedAt);
        await Assert.That(result.ReporterContactConsent).IsTrue();

        var serialized = JsonSerializer.Serialize(result);
        await Assert.That(serialized).DoesNotContain("encrypted-sensitive-reporter-text");
        await Assert.That(serialized).DoesNotContain("Moderator");
        await Assert.That(serialized).DoesNotContain("ReporterIpHash");
    }

    [Test]
    public async Task Handle_WhenCurrentUserIsMissing_ReturnsNullWithoutRepositoryLookup()
    {
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _currentUserService.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new GetMyReportRequest { ReportId = Guid.CreateVersion7() },
            CancellationToken.None);

        await Assert.That(result).IsNull();
        await _eventReportRepository.DidNotReceive().GetByIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenReportBelongsToAnotherUser_ReturnsNull()
    {
        var tenantId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var report = CreateReport(tenantId, Guid.CreateVersion7(), Guid.NewGuid(), DateTime.UtcNow);
        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(currentUserId);
        _eventReportRepository.GetByIdAsync(tenantId, report.Id, Arg.Any<CancellationToken>())
            .Returns(report);

        var result = await CreateHandler().Handle(
            new GetMyReportRequest { ReportId = report.Id },
            CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Handle_WhenSameUserRequestsSameReportTwice_UsesUserScopedCache()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var report = CreateReport(tenantId, Guid.CreateVersion7(), userId, DateTime.UtcNow);
        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(userId);
        _eventReportRepository.GetByIdAsync(tenantId, report.Id, Arg.Any<CancellationToken>())
            .Returns(report);
        var handler = CreateHandler();

        var first = await handler.Handle(new GetMyReportRequest { ReportId = report.Id }, CancellationToken.None);
        var second = await handler.Handle(new GetMyReportRequest { ReportId = report.Id }, CancellationToken.None);

        await Assert.That(first!.Id).IsEqualTo(second!.Id);
        await _eventReportRepository.Received(1).GetByIdAsync(tenantId, report.Id, Arg.Any<CancellationToken>());
        await Assert.That(_cache.FactoryCallsByKey.Values.Single()).IsEqualTo(1);
        await Assert.That(_cache.FactoryCallsByKey.Keys.Single()).Contains(userId.ToString("N"));
    }

    [Test]
    public async Task Handle_WhenDifferentUsersRequestSameReport_UsesSeparateCacheEntries()
    {
        var tenantId = Guid.NewGuid();
        var reporterUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var currentUserId = reporterUserId;
        var report = CreateReport(tenantId, Guid.CreateVersion7(), reporterUserId, DateTime.UtcNow);
        _tenantContext.TenantId.Returns(tenantId);
        _currentUserService.UserId.Returns(_ => currentUserId);
        _eventReportRepository.GetByIdAsync(tenantId, report.Id, Arg.Any<CancellationToken>())
            .Returns(report);
        var handler = CreateHandler();

        var ownResult = await handler.Handle(new GetMyReportRequest { ReportId = report.Id }, CancellationToken.None);
        currentUserId = otherUserId;
        var otherResult = await handler.Handle(new GetMyReportRequest { ReportId = report.Id }, CancellationToken.None);

        await Assert.That(ownResult).IsNotNull();
        await Assert.That(otherResult).IsNull();
        await _eventReportRepository.Received(2).GetByIdAsync(tenantId, report.Id, Arg.Any<CancellationToken>());
        await Assert.That(_cache.FactoryCallsByKey.Keys).Contains($"event-reporting:my-report:{tenantId:N}:{reporterUserId:N}:{report.Id:N}");
        await Assert.That(_cache.FactoryCallsByKey.Keys).Contains($"event-reporting:my-report:{tenantId:N}:{otherUserId:N}:{report.Id:N}");
    }

    private GetMyReportRequestHandler CreateHandler()
    {
        return new GetMyReportRequestHandler(
            _eventReportRepository,
            _tenantContext,
            _currentUserService,
            _cache);
    }

    private static EventReport CreateReport(Guid tenantId, Guid eventId, Guid reporterUserId, DateTime createdAt)
    {
        return EventReport.Create(
            tenantId,
            eventId,
            reporterUserId,
            Guid.CreateVersion7(),
            EventReporterKind.AuthenticatedUser,
            EventReportSourceKind.UserReport,
            "spam",
            "organizer",
            EventReportPriority.Normal,
            null,
            true,
            "en",
            "ip-hash",
            "ua-hash",
            createdAt);
    }

    private sealed class TestHybridCache : HybridCache
    {
        private readonly Dictionary<string, object?> _values = new();
        private readonly Dictionary<string, int> _factoryCallsByKey = new();

        public Dictionary<string, int> FactoryCallsByKey => _factoryCallsByKey;

        public override async ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            if (_values.ContainsKey(key))
            {
                return (T)_values[key]!;
            }

            _factoryCallsByKey[key] = _factoryCallsByKey.GetValueOrDefault(key) + 1;
            var created = await factory(state, cancellationToken);
            _values[key] = created;
            return created;
        }

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _values.Remove(key);
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public override ValueTask SetAsync<T>(
            string key,
            T value,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            _values[key] = value;
            return ValueTask.CompletedTask;
        }
    }
}
