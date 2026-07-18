// ABOUTME: Converts committed attendee registration lifecycle into a typed ATProto RSVP operation.
// ABOUTME: Ignores organizer approval state and emits only going, delete, or no operation.

using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Domain;

namespace Explore.Application.Features.Federation.Atproto.Services;

public static class AtprotoRsvpPublicationSnapshotFactory
{
    public const string GoingStatus = "community.lexicon.calendar.rsvp#going";

    public static AtprotoRsvpPublicationPlan PlanActiveRegistration(
        EventRegistrationIntent intent,
        AtprotoRsvpPublicationContext context,
        string ownerDid,
        AtprotoSettledEventReference? settledEvent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        if (intent.IsDeleted)
        {
            return Invalid("A deleted registration intent cannot create an RSVP.");
        }

        if (intent.Id == Guid.Empty
            || intent.TenantId == Guid.Empty
            || intent.UserId == Guid.Empty
            || intent.EventId == Guid.Empty
            || intent.CreatedAt == default)
        {
            return Invalid("Only a persisted committed registration intent can create an RSVP.");
        }

        if (context.TenantId == Guid.Empty
            || context.UserId == Guid.Empty
            || context.EventId == Guid.Empty
            || intent.TenantId != context.TenantId
            || intent.UserId != context.UserId
            || intent.EventId != context.EventId)
        {
            return Invalid("The registration intent does not match the requested tenant, user, and event scope.");
        }

        if (!IsDid(ownerDid))
        {
            return Invalid("A valid owner DID is required.");
        }

        if (settledEvent is null
            || !IsAtUri(settledEvent.Uri)
            || string.IsNullOrWhiteSpace(settledEvent.Cid))
        {
            return Invalid("A settled event URI and CID are required before RSVP publication.");
        }

        return new(
            AtprotoRsvpPublicationOperation.CreateOrUpdate,
            new(ownerDid.Trim(), settledEvent.Uri.Trim(), settledEvent.Cid.Trim(), GoingStatus),
            []);
    }

    public static AtprotoRsvpPublicationPlan PlanCancellation(
        int remainingActiveRegistrationCount,
        bool remoteRsvpExists)
    {
        if (remainingActiveRegistrationCount < 0)
        {
            return Invalid("The remaining active registration count cannot be negative.");
        }

        return remainingActiveRegistrationCount == 0 && remoteRsvpExists
            ? new(AtprotoRsvpPublicationOperation.Delete, null, [])
            : new(AtprotoRsvpPublicationOperation.None, null, []);
    }

    private static AtprotoRsvpPublicationPlan Invalid(string error)
        => new(AtprotoRsvpPublicationOperation.None, null, [error]);

    private static bool IsDid(string value)
        => !string.IsNullOrWhiteSpace(value)
            && value.StartsWith("did:", StringComparison.Ordinal)
            && value.IndexOfAny([' ', '\r', '\n', '\t']) < 0;

    private static bool IsAtUri(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !value.StartsWith("at://", StringComparison.Ordinal)
            || value.IndexOfAny([' ', '\r', '\n', '\t', '?', '#']) >= 0)
        {
            return false;
        }

        string[] parts = value[5..].Split('/', StringSplitOptions.None);
        return parts.Length == 3
            && IsDid(parts[0])
            && IsNsid(parts[1])
            && parts[2].Length > 0;
    }

    private static bool IsNsid(string value)
        => value.Length > 0
            && value.Contains('.', StringComparison.Ordinal)
            && value.All(character => char.IsAsciiLetterOrDigit(character)
                || character is '.' or '-');
}
