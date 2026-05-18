// ABOUTME: Command request for irreversible audited purge of dependency-free session custom-property definitions.
// ABOUTME: Blocks purge when values, projections, audit, or template provenance would lose history.

using Explore.Application.Authorization;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomProperties.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public sealed class PurgeEventSessionCustomPropertyDefinitionCommand : IRequest<BaseCommandResponse<CustomPropertyPurgeResultDto>>, ISecureRequest
{
    public Guid Id { get; set; }
    public required string Reason { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
