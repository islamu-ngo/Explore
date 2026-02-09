using System;

namespace Explore.Application.DTOs.User;

public class UpdateUserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public Guid? ProfilePictureId { get; set; }
}
