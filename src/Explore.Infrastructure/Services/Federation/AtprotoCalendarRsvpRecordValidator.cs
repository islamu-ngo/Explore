// ABOUTME: Semantically validates privacy-minimal community RSVP records before PDS publication.
// ABOUTME: Allows only going with a settled event strongRef and rejects unsupported user-intent states.

using System.Collections.Immutable;
using CarpaNet;
using CommunityLexicon.Calendar;
using Explore.Application.Features.Federation.Atproto.Services;

namespace Explore.Infrastructure.Services.Federation;

public static class AtprotoCalendarRsvpRecordValidator
{
    public static AtprotoCalendarRecordValidationResult Validate(Rsvp record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var errors = ImmutableArray.CreateBuilder<string>();
        if (!string.Equals(record.Status, AtprotoRsvpPublicationSnapshotFactory.GoingStatus, StringComparison.Ordinal))
        {
            errors.Add("Only the community RSVP going status is supported.");
        }

        if (record.Subject is null
            || !string.Equals(record.Subject.Uri.Collection, Event.RecordType, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(record.Subject.Uri.RecordKey))
        {
            errors.Add("The RSVP subject must be a settled community event AT URI.");
        }

        if (record.Subject is null || !new ATCid(record.Subject.Cid).IsAtProtoBlessedFormat)
        {
            errors.Add("The RSVP subject CID must use the ATProto DAG-CBOR SHA-256 format.");
        }

        AtprotoEncodedSizeValidationResult? size = null;
        if (errors.Count == 0)
        {
            size = AtprotoRecordSizeValidator.Validate(record);
            errors.AddRange(size.Errors);
        }

        return new(errors.ToImmutable(), size);
    }
}
