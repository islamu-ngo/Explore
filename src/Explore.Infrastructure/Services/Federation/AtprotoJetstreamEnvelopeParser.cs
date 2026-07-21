// ABOUTME: Converts bounded CarpaNet Jetstream envelopes into canonical records or payload-free quarantine outcomes.
// ABOUTME: Enforces exact collections, operations, DIDs, record types, generated lexicon shape, and encoded sizes.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CarpaNet;
using CarpaNet.Jetstream;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Federation;
using CalendarEvent = CommunityLexicon.Calendar.Event;
using CalendarRsvp = CommunityLexicon.Calendar.Rsvp;

namespace Explore.Infrastructure.Services.Federation;

internal static class AtprotoJetstreamConstants
{
    public const string EventCollection = "community.lexicon.calendar.event";
    public const string RsvpCollection = "community.lexicon.calendar.rsvp";
    public static readonly IReadOnlyList<string> Collections = [EventCollection, RsvpCollection];
}

internal sealed record AtprotoJetstreamParsedEnvelope(
    long Cursor,
    AtprotoRecord? Record,
    AtprotoJetstreamQuarantine? Quarantine,
    bool AdvanceCursor = true,
    AtprotoEventProjection? EventProjection = null,
    AtprotoEventProjectionInvalidation? EventProjectionInvalidation = null);

internal static class AtprotoJetstreamEnvelopeParser
{
    private static readonly HashSet<string> RsvpStatuses =
    [
        "community.lexicon.calendar.rsvp#interested",
        "community.lexicon.calendar.rsvp#going",
        "community.lexicon.calendar.rsvp#notgoing"
    ];

    public static AtprotoJetstreamParsedEnvelope Parse(
        JetstreamEvent envelope,
        long currentCursor,
        IReadOnlyCollection<string> allowedDids,
        DateTime observedAt)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (observedAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The observation timestamp must be UTC.", nameof(observedAt));
        }

        string recordJson = envelope.Commit?.Record?.GetRawText() ?? string.Empty;
        string envelopeHash = Hash(BuildEnvelopeFingerprint(envelope, recordJson));
        string? identityHash = BuildIdentityHash(envelope);
        bool cursorIsInRange = TryFromUnixMicroseconds(envelope.TimeUs, out DateTime parsedAt);
        DateTime eventAt = cursorIsInRange ? parsedAt : observedAt;
        AtprotoJetstreamParsedEnvelope Reject(
            string reason,
            bool advanceCursor = true,
            bool invalidateEventProjection = false) => new(
                envelope.TimeUs,
                null,
                new AtprotoJetstreamQuarantine
                {
                    Id = Guid.CreateVersion7(),
                    ReasonCode = reason,
                    EnvelopeHash = envelopeHash,
                    RecordIdentityHash = identityHash,
                    EventAt = eventAt,
                    QuarantinedAt = observedAt
                },
                advanceCursor,
                EventProjectionInvalidation: invalidateEventProjection
                    ? new AtprotoEventProjectionInvalidation(
                        envelope.Did,
                        envelope.Commit!.Collection!,
                        envelope.Commit.Rkey!,
                        envelope.TimeUs)
                    : null);

        if (envelope.TimeUs <= currentCursor || envelope.TimeUs <= 0 || !cursorIsInRange)
        {
            return Reject("invalid_cursor", advanceCursor: false);
        }

        if (!string.Equals(envelope.Kind, "commit", StringComparison.Ordinal) || envelope.Commit is null)
        {
            return Reject("unsupported_envelope_kind");
        }

        if (!IsValidDid(envelope.Did))
        {
            return Reject("invalid_did");
        }

        if (allowedDids.Count > 0 && !allowedDids.Contains(envelope.Did, StringComparer.Ordinal))
        {
            return Reject("did_not_allowed");
        }

        JetstreamCommit commit = envelope.Commit;
        if (commit.Collection is not AtprotoJetstreamConstants.EventCollection and not AtprotoJetstreamConstants.RsvpCollection)
        {
            return Reject("collection_not_allowed");
        }

        if (commit.Operation is not ("create" or "update" or "delete"))
        {
            return Reject("operation_not_supported");
        }

        if (!IsValidRecordKey(commit.Rkey))
        {
            return Reject("invalid_record_key");
        }

        string uri = $"at://{envelope.Did}/{commit.Collection}/{commit.Rkey}";
        if (uri.Length > 500)
        {
            return Reject("record_identity_too_large");
        }

        if (commit.Operation == "delete")
        {
            if (commit.Record.HasValue && commit.Record.Value.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
            {
                return Reject(
                    "invalid_delete_shape",
                    invalidateEventProjection: commit.Collection == AtprotoJetstreamConstants.EventCollection);
            }

            return new(envelope.TimeUs, CreateRecord(envelope, commit, uri, null, null, null, observedAt, eventAt), null);
        }

        if (!commit.Record.HasValue || commit.Record.Value.ValueKind != JsonValueKind.Object)
        {
            return Reject(
                "record_missing",
                invalidateEventProjection: commit.Collection == AtprotoJetstreamConstants.EventCollection);
        }

        if (Encoding.UTF8.GetByteCount(recordJson) > AtprotoRecordSizeValidator.MaximumJsonBytes)
        {
            return Reject(
                "record_too_large",
                invalidateEventProjection: commit.Collection == AtprotoJetstreamConstants.EventCollection);
        }

        JsonElement record = commit.Record.Value;
        if (!record.TryGetProperty("$type", out JsonElement type)
            || type.ValueKind != JsonValueKind.String
            || !string.Equals(type.GetString(), commit.Collection, StringComparison.Ordinal))
        {
            return Reject(
                "record_type_mismatch",
                invalidateEventProjection: commit.Collection == AtprotoJetstreamConstants.EventCollection);
        }

        if (commit.Cid is not { Length: > 0 and <= 255 } || !new ATCid(commit.Cid).IsAtProtoBlessedFormat)
        {
            return Reject(
                "invalid_record_cid",
                invalidateEventProjection: commit.Collection == AtprotoJetstreamConstants.EventCollection);
        }

        if (!TryValidateRecord(
                commit.Collection,
                record,
                out string? subjectUri,
                out string? subjectCid,
                out CalendarEvent? calendarEvent))
        {
            return Reject(
                "invalid_record_shape",
                invalidateEventProjection: commit.Collection == AtprotoJetstreamConstants.EventCollection);
        }

        AtprotoRecord canonicalRecord = CreateRecord(
            envelope,
            commit,
            uri,
            recordJson,
            subjectUri,
            subjectCid,
            observedAt,
            eventAt);
        return new(
            envelope.TimeUs,
            canonicalRecord,
            null,
            EventProjection: calendarEvent is null
                ? null
                : AtprotoCalendarEventProjectionMapper.Map(
                    calendarEvent,
                    canonicalRecord.Id,
                    envelope.TimeUs,
                    observedAt));
    }

    private static bool TryValidateRecord(
        string collection,
        JsonElement record,
        out string? subjectUri,
        out string? subjectCid,
        out CalendarEvent? calendarEvent)
    {
        subjectUri = null;
        subjectCid = null;
        calendarEvent = null;
        try
        {
            if (collection == AtprotoJetstreamConstants.EventCollection)
            {
                calendarEvent = CalendarEvent.FromJson(record);
                return calendarEvent is not null && AtprotoCalendarEventRecordValidator.Validate(calendarEvent).IsValid;
            }

            CalendarRsvp? rsvp = CalendarRsvp.FromJson(record);
            string? rsvpUri = rsvp?.Subject?.Uri.Value;
            string? rsvpCid = rsvp?.Subject?.Cid;
            if (rsvp?.Subject is null
                || !RsvpStatuses.Contains(rsvp.Status)
                || rsvp.Subject.Uri.Collection != AtprotoJetstreamConstants.EventCollection
                || string.IsNullOrWhiteSpace(rsvp.Subject.Uri.RecordKey)
                || rsvpUri is not { Length: > 0 and <= 500 }
                || rsvpCid is not { Length: > 0 and <= 255 }
                || !new ATCid(rsvpCid).IsAtProtoBlessedFormat
                || !AtprotoRecordSizeValidator.Validate(rsvp).IsValid)
            {
                return false;
            }

            subjectUri = rsvpUri;
            subjectCid = rsvpCid;
            return true;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException or FormatException)
        {
            return false;
        }
    }

    private static AtprotoRecord CreateRecord(
        JetstreamEvent envelope,
        JetstreamCommit commit,
        string uri,
        string? recordJson,
        string? subjectUri,
        string? subjectCid,
        DateTime observedAt,
        DateTime eventAt) => new()
        {
            Id = Guid.CreateVersion7(),
            Did = envelope.Did,
            Collection = commit.Collection!,
            RecordKey = commit.Rkey!,
            Cid = commit.Cid,
            Uri = uri,
            Direction = AtprotoRecordDirection.Inbound,
            Provenance = AtprotoRecordProvenance.Jetstream,
            SourceVersion = envelope.TimeUs,
            SourceCursor = envelope.TimeUs,
            RecordJson = recordJson,
            RecordHash = recordJson is null ? null : Hash(recordJson),
            SubjectUri = subjectUri,
            SubjectCid = subjectCid,
            IndexedAt = eventAt,
            UpdatedAt = observedAt,
            TombstonedAt = commit.Operation == "delete" ? observedAt : null
        };

    private static bool IsValidDid(string did)
    {
        try
        {
            return did.Length <= 255 && ATDid.IsValid(did);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool IsValidRecordKey(string? recordKey) =>
        recordKey is { Length: > 0 and <= 255 }
        && recordKey.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_' or ':' or '~');

    private static bool TryFromUnixMicroseconds(long value, out DateTime result)
    {
        try
        {
            result = DateTime.UnixEpoch.AddTicks(checked(value * 10));
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            result = default;
            return false;
        }
        catch (OverflowException)
        {
            result = default;
            return false;
        }
    }

    private static string BuildEnvelopeFingerprint(JetstreamEvent envelope, string recordJson) =>
        string.Join('\n', envelope.Did, envelope.TimeUs, envelope.Kind, envelope.Commit?.Operation,
            envelope.Commit?.Collection, envelope.Commit?.Rkey, envelope.Commit?.Cid, recordJson);

    private static string? BuildIdentityHash(JetstreamEvent envelope) =>
        envelope.Commit?.Collection is null || envelope.Commit.Rkey is null
            ? null
            : Hash(string.Join('\n', envelope.Did, envelope.Commit.Collection, envelope.Commit.Rkey));

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
