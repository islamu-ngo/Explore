// ABOUTME: MediatR command to update the user's last active tenant ID.
// ABOUTME: Returns a boolean indicating success.

using System;
using MediatR;

namespace Explore.Application.Features.Users.Requests.Commands;

public class UpdateUserLastActiveTenantCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
}
