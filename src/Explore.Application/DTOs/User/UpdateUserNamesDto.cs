// ABOUTME: Sub-DTO for updating a user's first and last name.
// ABOUTME: Carried optionally inside UpdateUserDto for partial profile updates.
namespace Explore.Application.DTOs.User;

public sealed record UpdateUserNamesDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}
