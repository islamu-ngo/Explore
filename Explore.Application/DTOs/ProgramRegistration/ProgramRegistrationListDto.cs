using System;

namespace Explore.Application.DTOs.ProgramRegistration
{
    public class ProgramRegistrationListDto
    {
        public Guid Id { get; set; }
        public Guid ProgramId { get; set; }
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
        public string Status { get; set; } = string.Empty;

        // Program snapshot
        public string ProgramTitle { get; set; } = string.Empty;
        public string ProgramDescription { get; set; } = string.Empty;
        public string? ProgramCity { get; set; }
        public string? ProgramAddress { get; set; }
        public string? ProgramUrl { get; set; }
        public DateTime? EventStartDate { get; set; }
        public DateTime? EventEndDate { get; set; }
    }
}
