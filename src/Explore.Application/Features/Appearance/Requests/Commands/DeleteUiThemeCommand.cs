// ABOUTME: Command for deleting a UI theme from the catalog by id.
// ABOUTME: Authorization and default-theme protection are enforced in the handler.

namespace Explore.Application.Features.Appearance.Requests.Commands;

using MediatR;

public sealed record DeleteUiThemeCommand(Guid Id = default) : IRequest<bool>;
