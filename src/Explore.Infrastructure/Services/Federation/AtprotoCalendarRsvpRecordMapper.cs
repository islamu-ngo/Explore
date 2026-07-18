// ABOUTME: Maps the privacy-minimal application RSVP snapshot to the generated community RSVP record.
// ABOUTME: Preserves the settled event URI/CID exactly and accepts only the supported going intent.

using CarpaNet;
using CommunityLexicon.Calendar;
using Explore.Application.Features.Federation.Atproto.Models;

namespace Explore.Infrastructure.Services.Federation;

public static class AtprotoCalendarRsvpRecordMapper
{
    public static Rsvp Map(AtprotoRsvpPublicationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new()
        {
            Subject = new()
            {
                Uri = new ATUri(snapshot.SubjectUri),
                Cid = snapshot.SubjectCid
            },
            Status = snapshot.Status
        };
    }
}
