using System;

namespace Explore.Application.DTOs.EventRegistration;

public class UpdateEventRegistrationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid EventSessionId { get; set; }
    public int? ApprovalStatusId { get; set; }
    public Guid TenantId { get; set; }
    public Guid? AtprotoRecordId { get; set; }
}
