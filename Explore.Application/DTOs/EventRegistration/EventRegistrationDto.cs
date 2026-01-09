using System;

namespace Explore.Application.DTOs.EventRegistration
{
    public class EventRegistrationDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? UserFirstName { get; set; }
        public string? UserLastName { get; set; }
        public string? UserEmail { get; set; }
        public Guid EventSessionId { get; set; }
        public string? EventSessionTitle { get; set; }
        public int? ApprovalStatusId { get; set; }
        public string? ApprovalStatusFullName { get; set; }
        public string? ApprovalStatusMasterCode { get; set; } // For i18n with Tolgee
        public Guid TenantId { get; set; }
        public Guid? AtprotoRecordId { get; set; }
    }
}
