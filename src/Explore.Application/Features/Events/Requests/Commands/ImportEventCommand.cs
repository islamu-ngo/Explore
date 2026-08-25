// ABOUTME: MediatR command for importing an event from an external source or backfill.
// ABOUTME: Supplies tenant-scoped resource context for authorization before the import handler runs.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Create)]
public sealed record ImportEventCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required ImportEventRequestDto Request { get; init; }
    public Guid TenantId { get; init; }

    string? ISecureRequest.ResourceId => TenantId.ToString();

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new TenantScopedAuthorizationFacts(TenantId);
}
