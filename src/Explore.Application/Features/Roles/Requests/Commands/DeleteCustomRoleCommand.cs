// ABOUTME: Command to delete a custom (non-system) role.
// ABOUTME: Validates no active members are assigned before deletion.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Roles.Requests.Commands;

public sealed record DeleteCustomRoleCommand(int RoleId = default) : IRequest<BaseCommandResponse<int>>;
