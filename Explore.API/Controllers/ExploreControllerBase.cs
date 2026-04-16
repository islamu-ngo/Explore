// ABOUTME: Abstract base controller providing centralized user identity access via IUserContext.
// ABOUTME: Eliminates duplicated GetCurrentUserId methods across controllers.

using Explore.Application.Contracts.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

public abstract class ExploreControllerBase : ControllerBase
{
    private IUserContext? _userContext;

    protected IUserContext UserContext => _userContext ??= HttpContext.RequestServices.GetRequiredService<IUserContext>();

    // Claim fallback: internal_user_id → sub → nameidentifier → sid
    protected Guid? CurrentUserId => UserContext.UserId;

    /// <exception cref="UnauthorizedAccessException">User is not authenticated.</exception>
    protected Guid RequiredUserId => UserContext.GetRequiredUserId();
}
