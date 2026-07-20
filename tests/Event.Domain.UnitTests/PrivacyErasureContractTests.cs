// ABOUTME: Specifies the platform-wide typed privacy-erasure fact, counter, and replay checkpoint invariants.
// ABOUTME: Rejects malformed identities, kinds, reasons, versions, timestamps, checkpoint chains, and instruction fields.

using System.Text.Json;
using Explore.Domain;

namespace Event.Domain.UnitTests;

public sealed class PrivacyErasureContractTests
{
    private static readonly DateTime RequestedAtUtc =
        new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task PrivacyErasureIntent_ValidUserFactIsImmutableAndTyped()
    {
        PrivacyErasureIntent intent = CreateIntent(1);

        await Assert.That(intent.IntentId.Version).IsEqualTo(7);
        await Assert.That(intent.AuthoritySequence).IsEqualTo(1);
        await Assert.That(intent.SubjectKind).IsEqualTo(PrivacyErasureSubjectKind.User);
        await Assert.That(intent.SubjectId).IsNotEqualTo(Guid.Empty);
        await Assert.That(intent.ReasonCode).IsEqualTo(PrivacyErasureReasonCode.AccountDeletion);
        await Assert.That(intent.PolicyVersion).IsEqualTo(1);
        await Assert.That(intent.RequestedAtUtc).IsEqualTo(RequestedAtUtc);
        await Assert.That(intent.RecordedAtUtc).IsEqualTo(RequestedAtUtc.AddSeconds(1));
        await Assert.That(intent.RetentionExpiresAtUtc)
            .IsGreaterThan(intent.RecordedAtUtc);
        await Assert.That(typeof(PrivacyErasureIntent).GetProperties()
            .Any(property => property.SetMethod?.IsPublic == true)).IsFalse();
    }

    [Test]
    public async Task PrivacyErasureIntent_RejectsMalformedIdentityKindReasonSequenceAndPolicy()
    {
        await Assert.That(() => CreateIntent(1, intentId: Guid.Empty)).Throws<ArgumentException>();
        await Assert.That(() => CreateIntent(1, intentId: Guid.NewGuid())).Throws<ArgumentException>();
        await Assert.That(() => CreateIntent(1, subjectKind: default)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => CreateIntent(1, subjectKind: (PrivacyErasureSubjectKind)2))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => CreateIntent(1, subjectId: Guid.Empty)).Throws<ArgumentException>();
        await Assert.That(() => CreateIntent(1, reasonCode: default)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => CreateIntent(1, reasonCode: (PrivacyErasureReasonCode)999))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => CreateIntent(0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => CreateIntent(-1)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => CreateIntent(1, policyVersion: 0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => CreateIntent(1, policyVersion: -1)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task PrivacyErasureIntent_RejectsDefaultNonUtcAndReversedTimestamps()
    {
        await Assert.That(() => PrivacyErasureIntent.Record(
                Guid.CreateVersion7(),
                1,
                PrivacyErasureSubjectKind.User,
                Guid.CreateVersion7(),
                PrivacyErasureReasonCode.AccountDeletion,
                1,
                default,
                RequestedAtUtc))
            .Throws<ArgumentException>();
        await Assert.That(() => CreateIntent(
                1,
                requestedAtUtc: DateTime.SpecifyKind(RequestedAtUtc, DateTimeKind.Unspecified)))
            .Throws<ArgumentException>();
        await Assert.That(() => CreateIntent(
                1,
                recordedAtUtc: DateTime.SpecifyKind(RequestedAtUtc, DateTimeKind.Local)))
            .Throws<ArgumentException>();
        await Assert.That(() => CreateIntent(
                1,
                requestedAtUtc: RequestedAtUtc.AddSeconds(2),
                recordedAtUtc: RequestedAtUtc.AddSeconds(1)))
            .Throws<ArgumentException>();
        await Assert.That(() => CreateIntent(
                1,
                retentionExpiresAtUtc: RequestedAtUtc))
            .Throws<ArgumentException>();
        await Assert.That(() => CreateIntent(
                1,
                retentionExpiresAtUtc: DateTime.SpecifyKind(
                    RequestedAtUtc.AddYears(1),
                    DateTimeKind.Unspecified)))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task PrivacyErasureCounter_AllocatesOnlyContiguousPositiveSequences()
    {
        PrivacyErasureCounter counter = PrivacyErasureCounter.Start();

        await Assert.That(counter.AllocateNext()).IsEqualTo(1);
        counter.AdvanceTo(2);
        await Assert.That(counter.LastSequence).IsEqualTo(2);
        await Assert.That(() => counter.AdvanceTo(2)).Throws<InvalidOperationException>();
        await Assert.That(() => counter.AdvanceTo(4)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task PrivacyErasureReplayCheckpoint_RejectsDuplicateGapAndMismatchedFacts()
    {
        PrivacyErasureIntent firstIntent = CreateIntent(1);
        PrivacyErasureIntent secondIntent = CreateIntent(2);
        PrivacyErasureReplayCheckpoint first = PrivacyErasureReplayCheckpoint.Start(
            firstIntent,
            RequestedAtUtc.AddMinutes(1),
            Guid.CreateVersion7());
        PrivacyErasureReplayCheckpoint second = PrivacyErasureReplayCheckpoint.Advance(
            first,
            secondIntent,
            RequestedAtUtc.AddMinutes(2),
            Guid.CreateVersion7());

        await Assert.That(second.Matches(secondIntent)).IsTrue();
        await Assert.That(second.Matches(CreateIntent(2, policyVersion: 2))).IsFalse();
        await Assert.That(() => PrivacyErasureReplayCheckpoint.Advance(
                second,
                secondIntent,
                RequestedAtUtc.AddMinutes(3)))
            .Throws<InvalidOperationException>();
        await Assert.That(() => PrivacyErasureReplayCheckpoint.Advance(
                second,
                CreateIntent(4),
                RequestedAtUtc.AddMinutes(3)))
            .Throws<InvalidOperationException>();
        await Assert.That(() => PrivacyErasureReplayCheckpoint.Start(
                CreateIntent(2),
                RequestedAtUtc.AddMinutes(1)))
            .Throws<InvalidOperationException>();
        await Assert.That(() => PrivacyErasureReplayCheckpoint.Start(
                firstIntent,
                RequestedAtUtc.AddMinutes(1),
                Guid.NewGuid()))
            .Throws<ArgumentException>();
        await Assert.That(() => PrivacyErasureReplayCheckpoint.Start(
                firstIntent,
                default,
                Guid.CreateVersion7()))
            .Throws<ArgumentException>();
        await Assert.That(() => PrivacyErasureReplayCheckpoint.Start(
                firstIntent,
                DateTime.SpecifyKind(RequestedAtUtc.AddMinutes(1), DateTimeKind.Unspecified),
                Guid.CreateVersion7()))
            .Throws<ArgumentException>();
        await Assert.That(() => PrivacyErasureReplayCheckpoint.Start(
                firstIntent,
                RequestedAtUtc.AddSeconds(-1),
                Guid.CreateVersion7()))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task PrivacyErasureIntent_ValidFactSerializesToBoundedPersistedShape()
    {
        PrivacyErasureIntent intent = CreateIntent(1);
        string[] allowedProperties = typeof(PrivacyErasureIntent)
            .GetProperties()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] forbiddenProperties = ["Table", "Column", "Sql", "Json", "Metadata"];
        string serialized = JsonSerializer.Serialize(intent);
        using JsonDocument validDocument = JsonDocument.Parse(serialized);
        string[] serializedProperties = validDocument.RootElement.EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        await Assert.That(serializedProperties).IsEquivalentTo(allowedProperties);
        await Assert.That(allowedProperties.Any(name => forbiddenProperties.Contains(
            name,
            StringComparer.OrdinalIgnoreCase))).IsFalse();
    }

    [Test]
    public async Task PrivacyErasureSagaAndCoverage_AreTypedHashedAndPolicyVersioned()
    {
        PrivacyErasureIntent intent = CreateIntent(1);
        DateTime fencedAtUtc = RequestedAtUtc.AddMinutes(1);
        byte[] receiptHash = new byte[32];
        PrivacyErasureSaga saga = PrivacyErasureSaga.Start(
            intent,
            1,
            receiptHash,
            fencedAtUtc.AddHours(24),
            fencedAtUtc,
            Guid.CreateVersion7());
        PrivacyErasurePolicyCoverage coverage = PrivacyErasurePolicyCoverage.Record(
            intent,
            2,
            fencedAtUtc.AddMinutes(1));

        await Assert.That(saga.SubjectKind).IsEqualTo(PrivacyErasureSubjectKind.User);
        await Assert.That(saga.ReceiptHash.Length).IsEqualTo(32);
        await Assert.That(saga.ReceiptExpiresAtUtc).IsGreaterThan(saga.FencedAtUtc);
        await Assert.That(coverage.IntentId).IsEqualTo(intent.IntentId);
        await Assert.That(coverage.SubjectKind).IsEqualTo(PrivacyErasureSubjectKind.User);
        await Assert.That(coverage.PolicyVersion).IsEqualTo(2);
        await Assert.That(() => PrivacyErasureSaga.Start(
                intent,
                0,
                receiptHash,
                fencedAtUtc.AddHours(24),
                fencedAtUtc))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => PrivacyErasureSaga.Start(
                intent,
                1,
                new byte[31],
                fencedAtUtc.AddHours(24),
                fencedAtUtc))
            .Throws<ArgumentException>();
        await Assert.That(() => PrivacyErasurePolicyCoverage.Record(
                intent,
                0,
                fencedAtUtc))
            .Throws<ArgumentOutOfRangeException>();
    }

    private static PrivacyErasureIntent CreateIntent(
        long authoritySequence,
        Guid? intentId = null,
        PrivacyErasureSubjectKind subjectKind = PrivacyErasureSubjectKind.User,
        Guid? subjectId = null,
        PrivacyErasureReasonCode reasonCode = PrivacyErasureReasonCode.AccountDeletion,
        int policyVersion = 1,
        DateTime? requestedAtUtc = null,
        DateTime? recordedAtUtc = null,
        DateTime? retentionExpiresAtUtc = null) =>
        PrivacyErasureIntent.Record(
            intentId ?? Guid.CreateVersion7(),
            authoritySequence,
            subjectKind,
            subjectId ?? Guid.CreateVersion7(),
            reasonCode,
            policyVersion,
            requestedAtUtc ?? RequestedAtUtc,
            recordedAtUtc ?? RequestedAtUtc.AddSeconds(1),
            retentionExpiresAtUtc);
}
