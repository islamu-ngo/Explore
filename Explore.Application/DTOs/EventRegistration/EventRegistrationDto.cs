using System;

namespace Explore.Application.DTOs.EventRegistration
{
    public class EventRegistrationDto
    {
        public Guid Id { get; set; }
        
        // User
        public Guid UserId { get; set; }
        public string? UserFullName { get; set; }
        public string? UserEmail { get; set; }
        
        // Event Session
        public Guid EventSessionId { get; set; }
        public string? EventSessionTitle { get; set; }
        
        // Registration Mode
        public int? RegistrationModeId { get; set; }
        public string? RegistrationModeFullName { get; set; }
        
        // Approval Status
        public int? ApprovalStatusId { get; set; }
        public string? ApprovalStatusFullName { get; set; }
        public string? ApprovalStatusMasterCode { get; set; }
        
        // Tenant
        public Guid TenantId { get; set; }
        
        // ATProto
        public Guid? AtprotoRecordId { get; set; }
    }
}
