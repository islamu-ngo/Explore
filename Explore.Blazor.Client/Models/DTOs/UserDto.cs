namespace Explore.Blazor.Client.Models.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        
        // S3 Image Key for the user profile picture
        public string? ProfileImageKey { get; set; }
    }
}
