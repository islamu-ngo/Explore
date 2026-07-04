// ABOUTME: Event registration list DTO for authenticated self-service collection reads.
// ABOUTME: Keeps owner context internal while exposing event/session status for clients.

using System;
using System.Text.Json.Serialization;

namespace Explore.Application.DTOs.EventRegistration;

public class EventRegistrationListDto
{
    public Guid Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }

    [JsonIgnore]
    public Guid UserId { get; set; }

    // Registration Intent (parent aggregate)
    public Guid? EventRegistrationIntentId { get; set; }

    // Event Session
    public Guid EventSessionId { get; set; }
    public string? EventSessionTitle { get; set; }

    // Event
    public Guid EventId { get; set; }
    public string? EventTitle { get; set; }
    public string? EventFeaturedImageUri { get; set; }
    public DateTimeOffset? EventStartTime { get; set; }

    // Approval Status
    public int? ApprovalStatusId { get; set; }
    public string? ApprovalStatusFullName { get; set; }
    public string? ApprovalStatusMasterCode { get; set; } // For i18n with Tolgee

    // Tenant
    public Guid TenantId { get; set; }
}
