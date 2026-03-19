// ABOUTME: Query request for paginated shared Layer 3 custom-property definition lists.
// ABOUTME: Requires an entity-type scope so organization and group catalogs stay distinct.

using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.CustomPropertyDefinitions.Requests.Queries;

public class GetCustomPropertyDefinitionListRequest : IRequest<PaginatedResult<CustomPropertyDefinitionListDto>>
{
    public EntityTypeName EntityTypeName { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = PaginatedResult<CustomPropertyDefinitionListDto>.DefaultPageSize;
}
