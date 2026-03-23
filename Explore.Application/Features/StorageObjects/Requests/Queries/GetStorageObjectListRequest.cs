// ABOUTME: MediatR query request for fetching a paginated storage object list.
// ABOUTME: Returns IEnumerable<StorageObjectListDto>.
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Queries;

public class GetStorageObjectListRequest : IRequest<PaginatedResult<StorageObjectListDto>>
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
