// ABOUTME: Authorized projection of an event organizer claim and its review state.
// ABOUTME: Exposes normalized status metadata and optimistic concurrency for claimant and curator flows.

using System.Text.Json.Serialization;

namespace Explore.Application.DTOs.EventOrganizerClaim;

public sealed class EventOrganizerClaimDto
{
    public Guid Id { get; set; }
    [JsonIgnore]
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid ClaimantActorId { get; set; }
    public string? ClaimantActorDisplayName { get; set; }
    public int StatusId { get; set; }
    public string? StatusCode { get; set; }
    public string? StatusName { get; set; }
    public required string EvidenceType { get; set; }
    public required string EvidenceReference { get; set; }
    public Guid? ReviewerUserId { get; set; }
    public string? DecisionReasonCode { get; set; }
    public DateTime? DecidedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid ConcurrencyStamp { get; set; }

    [JsonIgnore]
    public Guid EventActorId { get; set; }

    [JsonIgnore]
    public Guid? EventActorUserId { get; set; }

    [JsonIgnore]
    public Guid? EventActorOrganizationId { get; set; }

    [JsonIgnore]
    public Guid? EventActorGroupId { get; set; }

    [JsonIgnore]
    public int EventProvenanceTypeId { get; set; }

    [JsonIgnore]
    public string? EventProvenanceTypeCode { get; set; }

    [JsonIgnore]
    public Guid? EventOrganizerActorId { get; set; }

    [JsonIgnore]
    public Guid? EventSubmittedByUserId { get; set; }

    [JsonIgnore]
    public Guid? ClaimantActorUserId { get; set; }

    [JsonIgnore]
    public Guid? ClaimantActorOrganizationId { get; set; }

    [JsonIgnore]
    public Guid? ClaimantActorGroupId { get; set; }
}
