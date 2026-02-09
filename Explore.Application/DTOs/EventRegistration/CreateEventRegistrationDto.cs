using System;

namespace Explore.Application.DTOs.EventRegistration;

public class CreateEventRegistrationDto
{
    public Guid UserId { get; set; }
    public Guid EventSessionId { get; set; }
    public int? ApprovalStatusId { get; set; }
    public Guid TenantId { get; set; }
    public Guid? AtprotoRecordId { get; set; }
}
