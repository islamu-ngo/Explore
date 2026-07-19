// ABOUTME: Carries one typed organizer mutation of an EventLocation disclosure policy.
// ABOUTME: Requires both observed aggregate concurrency and policy-version tokens.

using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventLocations.Requests.Commands;

public sealed record UpdateEventLocationPolicyCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid EventId { get; init; }
    public Guid EventLocationId { get; init; }
    public Guid ExpectedConcurrencyStamp { get; init; }
    public int ExpectedPolicyVersion { get; init; }
    public EventLocationDisclosureFields SelectedFields { get; init; }
    public LocationDisclosureAudienceEnum FullDetailsAudience { get; init; }
    public DateTime? RevealFullDetailsFromUtc { get; init; }
    public bool NeedsPrivacyReview { get; init; }
}
