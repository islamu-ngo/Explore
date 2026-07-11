// ABOUTME: Event registration detail DTO for authenticated self-service reads.
// ABOUTME: Keeps owner context internal while exposing event/session status for PATCH flows.

using System;
using System.Text.Json.Serialization;

namespace Explore.Application.DTOs.EventRegistration;

public class EventRegistrationDto
{
    public Guid Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }

    [JsonIgnore]
    public Guid UserId { get; set; }

    // Event
    public Guid EventId { get; set; }
    public string? EventTitle { get; set; }

    // Registration Intent (parent aggregate)
    public Guid? EventRegistrationIntentId { get; set; }

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
