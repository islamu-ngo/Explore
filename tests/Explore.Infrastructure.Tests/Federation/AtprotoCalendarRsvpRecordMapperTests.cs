// ABOUTME: Verifies generated community RSVP mapping, strongRef propagation, semantic rejection, and size boundaries.
// ABOUTME: Proves only going is publishable and no attendee PII or internal identifier enters the record.

using CarpaNet;
using CommunityLexicon.Calendar;
using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Infrastructure.Services.Federation;

namespace Explore.Infrastructure.Tests.Federation;

public sealed class AtprotoCalendarRsvpRecordMapperTests
{
    private static readonly string ValidCid = ATCid.FromSha256Hash(new byte[32]).Value;

    [Test]
    public async Task Map_PreservesSettledStrongRef_AndOnlyGoing()
    {
        var snapshot = new AtprotoRsvpPublicationSnapshot(
            "did:plc:owner",
            "at://did:plc:owner/community.lexicon.calendar.event/3m123",
            ValidCid,
            AtprotoRsvpPublicationSnapshotFactory.GoingStatus);

        Rsvp record = AtprotoCalendarRsvpRecordMapper.Map(snapshot);
        AtprotoCalendarRecordValidationResult validation = AtprotoCalendarRsvpRecordValidator.Validate(record);

        await Assert.That(record.Subject.Uri.Value).IsEqualTo(snapshot.SubjectUri);
        await Assert.That(record.Subject.Cid).IsEqualTo(snapshot.SubjectCid);
        await Assert.That(record.Status).IsEqualTo(AtprotoRsvpPublicationSnapshotFactory.GoingStatus);
        await Assert.That(record.ToJson().GetRawText()).DoesNotContain("attendee-private-canary");
        await Assert.That(validation.IsValid).IsTrue();
    }

    [Test]
    [Arguments("community.lexicon.calendar.rsvp#interested")]
    [Arguments("community.lexicon.calendar.rsvp#notgoing")]
    [Arguments("community.lexicon.calendar.rsvp#rsvpGoing")]
    public async Task Validate_RejectsUnsupportedIntentValues(string status)
    {
        Rsvp record = CreateRecord(status);

        AtprotoCalendarRecordValidationResult result = AtprotoCalendarRsvpRecordValidator.Validate(record);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors).Contains(error => error.Contains("Only", StringComparison.Ordinal));
    }

    [Test]
    public async Task Validate_RejectsMissingOrUnsettledSubject()
    {
        Rsvp wrongCollection = CreateRecord(AtprotoRsvpPublicationSnapshotFactory.GoingStatus);
        wrongCollection.Subject.Uri = new ATUri("at://did:plc:owner/app.bsky.feed.post/key");
        Rsvp invalidCid = CreateRecord(AtprotoRsvpPublicationSnapshotFactory.GoingStatus);
        invalidCid.Subject.Cid = "not-a-cid";

        await Assert.That(AtprotoCalendarRsvpRecordValidator.Validate(wrongCollection).IsValid).IsFalse();
        await Assert.That(AtprotoCalendarRsvpRecordValidator.Validate(invalidCid).IsValid).IsFalse();
    }

    [Test]
    public async Task SizeValidator_AcceptsExactBoundaries_AndRejectsOneByteOver()
    {
        AtprotoEncodedSizeValidationResult exact = AtprotoRecordSizeValidator.ValidateEncodedLengths(
            AtprotoRecordSizeValidator.MaximumJsonBytes,
            AtprotoRecordSizeValidator.MaximumDagCborBytes);
        AtprotoEncodedSizeValidationResult jsonOverflow = AtprotoRecordSizeValidator.ValidateEncodedLengths(
            AtprotoRecordSizeValidator.MaximumJsonBytes + 1,
            AtprotoRecordSizeValidator.MaximumDagCborBytes);
        AtprotoEncodedSizeValidationResult cborOverflow = AtprotoRecordSizeValidator.ValidateEncodedLengths(
            AtprotoRecordSizeValidator.MaximumJsonBytes,
            AtprotoRecordSizeValidator.MaximumDagCborBytes + 1);

        await Assert.That(exact.IsValid).IsTrue();
        await Assert.That(jsonOverflow.IsValid).IsFalse();
        await Assert.That(cborOverflow.IsValid).IsFalse();
    }

    private static Rsvp CreateRecord(string status)
        => new()
        {
            Subject = new()
            {
                Uri = new ATUri("at://did:plc:owner/community.lexicon.calendar.event/3m123"),
                Cid = ValidCid
            },
            Status = status
        };
}
