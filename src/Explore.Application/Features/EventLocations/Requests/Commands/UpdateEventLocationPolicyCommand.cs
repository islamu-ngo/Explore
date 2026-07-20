// ABOUTME: Carries one typed organizer mutation of an EventLocation disclosure policy.
// ABOUTME: Requires both observed aggregate concurrency and policy-version tokens.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventLocations.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Update)]
public sealed record UpdateEventLocationPolicyCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventId { get; init; }
    public Guid EventLocationId { get; init; }
    public Guid ExpectedConcurrencyStamp { get; init; }
    public int ExpectedPolicyVersion { get; init; }
    public EventLocationDisclosureFields SelectedFields { get; init; }
    public LocationDisclosureAudienceEnum FullDetailsAudience { get; init; }
    public DateTime? RevealFullDetailsFromUtc { get; init; }
    public bool NeedsPrivacyReview { get; init; }

    string? ISecureRequest.ResourceId => EventId.ToString("D");
}

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Update)]
public sealed record ConfirmEventLocationRemediationCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventId { get; init; }
    public Guid EventLocationId { get; init; }
    public Guid ExpectedConcurrencyStamp { get; init; }
    public int ExpectedPolicyVersion { get; init; }

    string? ISecureRequest.ResourceId => EventId.ToString("D");
}
