using System;

namespace Explore.Application.DTOs.User
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        // TODO hold user location data? or fetch when user visit page and then used fetched location with user permission for when location is needed for whatever?
        //public string? City { get; set; }
        //public string? Country { get; set; }
    }
}
