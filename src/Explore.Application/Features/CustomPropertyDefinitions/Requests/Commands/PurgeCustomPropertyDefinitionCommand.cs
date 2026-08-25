// ABOUTME: Command request for irreversible audited purge of dependency-free shared custom-property definitions.
// ABOUTME: Keeps hard purge separate from normal retire + soft-delete lifecycle.

using Explore.Application.Authorization;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.CustomPropertyDefinitions.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public sealed record PurgeCustomPropertyDefinitionCommand : IRequest<BaseCommandResponse<CustomPropertyPurgeResultDto>>, ISecureRequest
{
    public Guid Id { get; init; }
    public required string Reason { get; init; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
