// ABOUTME: Wrapper DTO for partial user profile updates using nullable logical groups.
// ABOUTME: Body IDs are intentionally absent because update routes use the route ID as authoritative.

namespace Explore.Application.DTOs.User;

public class UpdateUserDto
{
    public UpdateUserNamesDto? Names { get; set; }
    public UpdateUserProfileImageDto? ProfileImage { get; set; }
}
