using System.Collections.Generic;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Requests.Queries;

public class GetEventRegistrationListRequest : IRequest<PaginatedResult<EventRegistrationListDto>>
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
