// ABOUTME: MediatR query to resolve admin authority for a specific user.
// Used by the UserController admin-authority endpoint to support the BFF claims transformation.

using Explore.Application.DTOs.User;
using MediatR;

namespace Explore.Application.Features.Users.Requests.Queries;

/// <summary>
/// Query to resolve the admin authority of a user across all hierarchy levels.
/// Returns <see cref="AdminAuthorityDto"/> with instance, tenant, and organization admin status.
/// </summary>
public class GetAdminAuthorityRequest : IRequest<AdminAuthorityDto>
{
    public Guid UserId { get; set; }
}
