// ABOUTME: Domain tests for safe event moderation history records.
// ABOUTME: Verifies reversible light moderation, irreversible redaction, and unsafe-content field exclusion.

using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Interfaces;

namespace Event.Domain.UnitTests.Entities;

public class EventModerationRecordTests
{
    [Test]
    public async Task EventModerationRecord_ImplementsTenantEntityInterface()
    {
        await Assert.That(typeof(EventModerationRecord).GetInterfaces().Contains(typeof(ITenantEntity))).IsTrue();
    }

    [Test]
    public async Task CreateLightModeration_AllowsUnmoderationAndStoresSafeMetadataOnly()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var moderatorUserId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var record = EventModerationRecord.CreateLightModeration(
            tenantId,
            eventId,
            moderatorUserId,
            "policy_review",
            (int)EventStatusEnum.Published,
            "correlation-123",
            createdAt);

        await Assert.That(record.TenantId).IsEqualTo(tenantId);
        await Assert.That(record.EventId).IsEqualTo(eventId);
        await Assert.That(record.ModeratorUserId).IsEqualTo(moderatorUserId);
        await Assert.That(record.ActionKind).IsEqualTo(EventModerationActionKind.LightModerated);
        await Assert.That(record.PreviousStatusId).IsEqualTo((int)EventStatusEnum.Published);
        await Assert.That(record.ResultingStatusId).IsEqualTo((int)EventStatusEnum.Moderated);
        await Assert.That(record.IsIrreversible).IsFalse();
        await Assert.That(record.AllowsUnmoderation).IsTrue();
        await Assert.That(record.ReasonCode).IsEqualTo("policy_review");
        await Assert.That(record.CorrelationId).IsEqualTo("correlation-123");
        await Assert.That(record.CreatedAt).IsEqualTo(createdAt);
        await Assert.That(UnsafePayloadProperties()).IsEmpty();
    }

    [Test]
    public async Task CreateHeavyRedaction_IsIrreversibleAndCannotBeUnmoderated()
    {
        var record = EventModerationRecord.CreateHeavyRedaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "illegal_content",
            (int)EventStatusEnum.Published,
            null,
            DateTimeOffset.UtcNow);

        await Assert.That(record.ActionKind).IsEqualTo(EventModerationActionKind.HeavyRedacted);
        await Assert.That(record.ResultingStatusId).IsEqualTo((int)EventStatusEnum.Moderated);
        await Assert.That(record.IsIrreversible).IsTrue();
        await Assert.That(record.AllowsUnmoderation).IsFalse();
    }

    [Test]
    public async Task CreateUnmoderation_RejectsIrreversibleModerationRecord()
    {
        var heavyRecord = EventModerationRecord.CreateHeavyRedaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "illegal_content",
            (int)EventStatusEnum.Published,
            null,
            DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
        {
            _ = EventModerationRecord.CreateUnmoderation(
                heavyRecord,
                Guid.NewGuid(),
                "review_complete",
                "correlation-456",
                DateTimeOffset.UtcNow);

            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task CreateUnmoderation_FromLightModerationReturnsPublishedStatus()
    {
        var lightRecord = EventModerationRecord.CreateLightModeration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "policy_review",
            (int)EventStatusEnum.Published,
            null,
            DateTimeOffset.UtcNow);

        var unmoderationRecord = EventModerationRecord.CreateUnmoderation(
            lightRecord,
            Guid.NewGuid(),
            "review_complete",
            null,
            DateTimeOffset.UtcNow);

        await Assert.That(unmoderationRecord.ActionKind).IsEqualTo(EventModerationActionKind.Unmoderated);
        await Assert.That(unmoderationRecord.PreviousStatusId).IsEqualTo((int)EventStatusEnum.Moderated);
        await Assert.That(unmoderationRecord.ResultingStatusId).IsEqualTo((int)EventStatusEnum.Published);
        await Assert.That(unmoderationRecord.IsIrreversible).IsFalse();
        await Assert.That(unmoderationRecord.AllowsUnmoderation).IsFalse();
    }

    private static IReadOnlyList<string> UnsafePayloadProperties()
    {
        string[] forbiddenFragments =
        [
            "Title",
            "Subtitle",
            "Description",
            "Content",
            "Slug",
            "Url",
            "Uri",
            "Image",
            "ObjectKey",
            "Payload"
        ];

        return typeof(EventModerationRecord)
            .GetProperties()
            .Select(property => property.Name)
            .Where(name => forbiddenFragments.Any(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
}
