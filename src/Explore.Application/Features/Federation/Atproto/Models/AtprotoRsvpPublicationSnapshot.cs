// ABOUTME: Typed privacy-minimal RSVP projection and lifecycle operation plan for ATProto publication.
// ABOUTME: Carries only owner DID, settled event strongRef values, and the supported going status.

using System.Collections.Immutable;

namespace Explore.Application.Features.Federation.Atproto.Models;

public sealed record AtprotoSettledEventReference(string Uri, string Cid);

public sealed record AtprotoRsvpPublicationContext(Guid TenantId, Guid UserId, Guid EventId);

public sealed record AtprotoRsvpPublicationSnapshot(
    string OwnerDid,
    string SubjectUri,
    string SubjectCid,
    string Status);

public enum AtprotoRsvpPublicationOperation
{
    None = 0,
    CreateOrUpdate = 1,
    Delete = 2
}

public sealed record AtprotoRsvpPublicationPlan(
    AtprotoRsvpPublicationOperation Operation,
    AtprotoRsvpPublicationSnapshot? Snapshot,
    ImmutableArray<string> Errors)
{
    public bool IsValid => Errors.IsEmpty;
}
