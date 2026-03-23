// ABOUTME: MediatR query request for fetching all session-language links.
// ABOUTME: Returns IEnumerable<EventSessionLanguageDto>.
using System.Collections.Generic;
using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Requests.Queries;

public class GetEventSessionLanguageListRequest : IRequest<PaginatedResult<EventSessionLanguageListDto>>
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
