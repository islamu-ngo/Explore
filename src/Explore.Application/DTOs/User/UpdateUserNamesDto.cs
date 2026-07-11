// ABOUTME: Sub-DTO for updating a user's first and last name.
// ABOUTME: Carried optionally inside UpdateUserDto for partial profile updates.
namespace Explore.Application.DTOs.User;

public class UpdateUserNamesDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
