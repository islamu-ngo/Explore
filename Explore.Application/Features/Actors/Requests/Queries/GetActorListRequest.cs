using MediatR;
using Explore.Application.DTOs.Actor;
using Explore.Application.Responses;

namespace Explore.Application.Features.Actors.Requests.Queries;

public class GetActorListRequest : IRequest<PaginatedResult<ActorListDto>>
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
