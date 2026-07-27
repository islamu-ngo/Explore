// ABOUTME: Route-identified command for patching an existing UI theme with optimistic concurrency.
// ABOUTME: Carries nullable logical groups so omitted theme properties remain unchanged.

namespace Explore.Application.Features.Appearance.Requests.Commands;

using Explore.Application.DTOs.Appearance;
using Explore.Application.Responses;
using MediatR;

public class UpdateUiThemeCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid Id { get; set; }
    public required UpdateUiThemeDto UiThemeDto { get; set; }
}
