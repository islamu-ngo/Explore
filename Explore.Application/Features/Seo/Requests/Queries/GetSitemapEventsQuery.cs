// ABOUTME: MediatR query for fetching public published events eligible for sitemap.xml.
// ABOUTME: Caps result count to the sitemap protocol limit while preserving tenant query filters.

using Explore.Application.DTOs.Seo;
using MediatR;

namespace Explore.Application.Features.Seo.Requests.Queries;

public sealed record GetSitemapEventsQuery(int MaxCount = 50_000) : IRequest<IReadOnlyList<SitemapEventEntryDto>>;
