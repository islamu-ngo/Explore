using System;

namespace Explore.Application.DTOs.EventRegistrationIntent;

public class EventRegistrationIntentListDto
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }
    public string? EventTitle { get; set; }

    public Guid UserId { get; set; }
    public string? UserFullName { get; set; }

    public int RegistrationScopeId { get; set; }
    public string? RegistrationScopeFullName { get; set; }
    public string? RegistrationScopeMasterCode { get; set; }

    public Guid? SelectedEventDayId { get; set; }

    public int? ApprovalStatusId { get; set; }
    public string? ApprovalStatusFullName { get; set; }
    public string? ApprovalStatusMasterCode { get; set; }

    public DateTime CreatedAt { get; set; }
}
