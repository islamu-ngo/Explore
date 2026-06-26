using System;

namespace Explore.Application.DTOs.User;

public class UpdateUserDto
{
    public Guid Id { get; set; }
    public UpdateUserNamesDto? Names { get; set; }
    public UpdateUserProfileImageDto? ProfileImage { get; set; }
}
