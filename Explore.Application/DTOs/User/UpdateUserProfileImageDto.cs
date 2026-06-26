// ABOUTME: Sub-DTO for updating a user's profile picture (links to Actor).
// ABOUTME: Carried optionally inside UpdateUserDto for partial profile updates.
using System;

namespace Explore.Application.DTOs.User;

public class UpdateUserProfileImageDto
{
    public Guid ProfilePictureId { get; set; }
}
