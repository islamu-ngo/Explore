// ABOUTME: Command request for updating an event-local custom-property definition.
// ABOUTME: Route ID and If-Match carry identity/concurrency; the body carries the update payload.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventCustomProperty;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCustomProperties.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public sealed record UpdateEventCustomPropertyDefinitionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid DefinitionId { get; init; }

    public required UpdateEventCustomPropertyDefinitionDto DefinitionDto { get; init; }

    public Guid ExpectedConcurrencyStamp { get; init; }

    public Guid TenantId { get; init; }

    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString();

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        TenantId == Guid.Empty
        ? null
        : new TenantScopedAuthorizationFacts(TenantId);
}
