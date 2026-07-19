// ABOUTME: Verifies exact-read audit materialization uses server UTC and identifier-only facts.
// ABOUTME: Proves repository failure or cancellation prevents audit completion.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public sealed class EventLocationExactReadAuditServiceTests
{
    [Test]
    [Category("EventLocationPrivacy")]
    [Category("Todo10EventLocationExactReadAudit")]
    public async Task RecordManyAsync_MixedDecisions_PersistsOneServerTimedPiiFreeBatch()
    {
        DateTimeOffset serverNow = new(2026, 7, 19, 15, 30, 0, TimeSpan.Zero);
        Guid tenantId = Guid.CreateVersion7();
        Guid requesterUserId = Guid.CreateVersion7();
        Guid correlationId = Guid.CreateVersion7();
        Guid traceId = Guid.CreateVersion7();
        var requests = new[]
        {
            new EventLocationExactReadAuditRequest(
                tenantId,
                Guid.CreateVersion7(),
                requesterUserId,
                EventLocationExactReadPurposeEnum.EventManagement,
                true,
                correlationId,
                null),
            new EventLocationExactReadAuditRequest(
                tenantId,
                Guid.CreateVersion7(),
                requesterUserId,
                EventLocationExactReadPurposeEnum.EventManagement,
                false,
                null,
                traceId)
        };
        var repository = Substitute.For<IEventLocationExactReadAuditRepository>();
        IReadOnlyCollection<EventLocationExactReadAudit>? observedAudits = null;
        repository.AppendManyAsync(
                Arg.Do<IReadOnlyCollection<EventLocationExactReadAudit>>(audits => observedAudits = audits),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var service = new EventLocationExactReadAuditService(repository, new FixedTimeProvider(serverNow));

        await service.RecordManyAsync(requests, CancellationToken.None);

        await Assert.That(observedAudits).IsNotNull();
        await Assert.That(observedAudits!).Count().IsEqualTo(2);
        EventLocationExactReadAudit[] audits = observedAudits.ToArray();
        await Assert.That(audits.Select(audit => audit.EventLocationId))
            .IsEquivalentTo(requests.Select(request => request.EventLocationId));
        await Assert.That(audits.Select(audit => audit.WasAuthorized)).IsEquivalentTo([true, false]);
        await Assert.That(audits.All(audit =>
            audit.Id.Version == 7
            && audit.TenantId == tenantId
            && audit.RequesterUserId == requesterUserId
            && audit.Purpose == EventLocationExactReadPurposeEnum.EventManagement
            && audit.OccurredAtUtc == serverNow.UtcDateTime)).IsTrue();
        await Assert.That(audits[0].CorrelationId).IsEqualTo(correlationId);
        await Assert.That(audits[0].TraceId).IsNotNull();
        await Assert.That(audits[1].CorrelationId).IsNull();
        await Assert.That(audits[1].TraceId).IsEqualTo(traceId);
        string[] persistedProperties = typeof(EventLocationExactReadAudit)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();
        await Assert.That(persistedProperties.Intersect(
            ["Address", "Postcode", "Latitude", "Longitude", "RoomName", "Description", "LocationPii"]))
            .IsEmpty();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Category("Todo10EventLocationExactReadAudit")]
    public async Task RecordManyAsync_RepositoryCancellation_PropagatesWithoutRetry()
    {
        using var source = new CancellationTokenSource();
        var repository = Substitute.For<IEventLocationExactReadAuditRepository>();
        repository.AppendManyAsync(
                Arg.Any<IReadOnlyCollection<EventLocationExactReadAudit>>(),
                Arg.Any<CancellationToken>())
            .Returns<Task>(_ =>
            {
                source.Cancel();
                throw new OperationCanceledException(source.Token);
            });
        var service = new EventLocationExactReadAuditService(repository, TimeProvider.System);
        var request = new EventLocationExactReadAuditRequest(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            EventLocationExactReadPurposeEnum.EventManagement,
            false,
            Guid.CreateVersion7());

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.RecordManyAsync([request], source.Token));
        await repository.Received(1).AppendManyAsync(
            Arg.Any<IReadOnlyCollection<EventLocationExactReadAudit>>(),
            source.Token);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
