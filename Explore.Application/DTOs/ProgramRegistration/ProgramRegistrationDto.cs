using System;

namespace Explore.Application.DTOs.ProgramRegistration
{
    public class ProgramRegistrationDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ProgramId { get; set; }
        public int StatusTypeId { get; set; }
        public string StatusTypeFullName { get; set; } = string.Empty;
        
        // Note: User detail fields (FirstName, LastName, Email, etc.) 
        // are collected via the form for UX but not stored in database
    }
}