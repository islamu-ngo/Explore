// ABOUTME: Requests the authenticated caller's server-authoritative workspace-shell context.
// ABOUTME: The handler derives identity and tenant from request-scoped contracts rather than client input.

using Explore.Application.DTOs.UiShell;
using MediatR;

namespace Explore.Application.Features.UiShell.Requests.Queries;

public sealed record GetUiShellContextRequest : IRequest<UiShellContextDto>;
