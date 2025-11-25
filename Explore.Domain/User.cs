using System;

namespace Explore.Domain
{
    public class User
    {
        public Guid Id { get; set; } // Keycloak Subject ID
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }
}
