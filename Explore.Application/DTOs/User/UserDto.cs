using System;

namespace Explore.Application.DTOs.User
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Username { get; set; }
        
        // Actor
        public Guid ActorId { get; set; }
        public string? ActorDisplayName { get; set; }
        public string? ActorHandle { get; set; }
        
        // Auth
        public string? AuthProvider { get; set; }
        public bool? EmailVerified { get; set; }
    }
}
