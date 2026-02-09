using System.Collections.Generic;
using Explore.Application.DTOs.Location;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Locations.Requests.Queries;

public class GetLocationListRequest : IRequest<PaginatedResult<LocationListDto>>
{
    /// <summary>
    /// Gets or sets the page number (1-based). Defaults to 1.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size. Defaults to 20.
    /// </summary>
    public int PageSize { get; set; } = 20;
}
