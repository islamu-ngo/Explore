// ABOUTME: Query for retrieving either the platform theme catalog or the current tenant-owned catalog.
// ABOUTME: Keeps administrative catalog reads separate from the runtime available-themes query.

namespace Explore.Application.Features.Appearance.Requests.Queries;

using Explore.Application.DTOs.Appearance;
using MediatR;

public sealed record GetUiThemeCatalogQuery(
    bool IsPlatformCatalog = default,
    bool ActiveOnly = default) : IRequest<IReadOnlyList<UiThemeListItemDto>>;
