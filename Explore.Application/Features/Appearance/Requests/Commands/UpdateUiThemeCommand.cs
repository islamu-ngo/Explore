// ABOUTME: Command for updating an existing UI theme with optimistic concurrency.
// ABOUTME: Carries the full edit DTO so the handler can validate scope ownership and stale edits.

namespace Explore.Application.Features.Appearance.Requests.Commands;

using Explore.Application.DTOs.Appearance;
using Explore.Application.Responses;
using MediatR;

public class UpdateUiThemeCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateUiThemeDto UiThemeDto { get; set; }
}
