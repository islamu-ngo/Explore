// ABOUTME: Query for retrieving either the platform theme catalog or the current tenant-owned catalog.
// ABOUTME: Keeps administrative catalog reads separate from the runtime available-themes query.

namespace Explore.Application.Features.Appearance.Requests.Queries;

using Explore.Application.DTOs.Appearance;
using MediatR;

public class GetUiThemeCatalogQuery : IRequest<IReadOnlyList<UiThemeListItemDto>>
{
    public bool IsPlatformCatalog { get; set; }
    public bool ActiveOnly { get; set; }
}
