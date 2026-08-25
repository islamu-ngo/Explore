// ABOUTME: Query for retrieving a single UI theme for administrative editing.
// ABOUTME: The handler resolves scope ownership and authorization from the stored entity.

namespace Explore.Application.Features.Appearance.Requests.Queries;

using Explore.Application.DTOs.Appearance;
using MediatR;

public sealed record GetUiThemeDetailsQuery(Guid Id = default) : IRequest<UiThemeDetailsDto?>;
