// ABOUTME: Query request for retrieving one shared Layer 3 custom-property definition with options.
// ABOUTME: Used by tenant-admin details flows for organization and group extension catalogs.

using Explore.Application.DTOs.CustomPropertyDefinition;
using MediatR;

namespace Explore.Application.Features.CustomPropertyDefinitions.Requests.Queries;

public class GetCustomPropertyDefinitionDetailsRequest : IRequest<CustomPropertyDefinitionDto>
{
    public Guid Id { get; set; }
}
