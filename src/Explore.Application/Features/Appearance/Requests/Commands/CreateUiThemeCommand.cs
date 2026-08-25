// ABOUTME: Command for creating a platform-owned or tenant-owned UI theme.
// ABOUTME: Leaves scope authorization to the handler so the current admin context remains authoritative.

namespace Explore.Application.Features.Appearance.Requests.Commands;

using Explore.Application.DTOs.Appearance;
using Explore.Application.Responses;
using MediatR;

public sealed record CreateUiThemeCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateUiThemeDto UiThemeDto { get; init; }
}
