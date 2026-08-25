// ABOUTME: Wrapper DTO for partial user profile updates using nullable logical groups.
// ABOUTME: Body IDs are intentionally absent because update routes use the route ID as authoritative.

namespace Explore.Application.DTOs.User;

public sealed record UpdateUserDto
{
    public UpdateUserNamesDto? Names { get; init; }
    public UpdateUserProfileImageDto? ProfileImage { get; init; }
}
