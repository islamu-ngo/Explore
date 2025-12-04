using System;

namespace Explore.Application.DTOs.ProgramRegistration
{
    public class CreateProgramRegistrationDto
    {
        public Guid ProgramId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Organization { get; set; }
        public string? Message { get; set; }
        public bool AcceptTerms { get; set; }
    }
}