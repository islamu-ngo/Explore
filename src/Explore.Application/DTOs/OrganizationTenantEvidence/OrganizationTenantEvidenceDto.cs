// ABOUTME: Safe authenticated projection of OrganizationTenant legitimacy evidence and review state.
// ABOUTME: Exposes application document identity and display metadata without provider keys, locators, content, or reviewer identity.

using System.Text.Json.Serialization;

namespace Explore.Application.DTOs.OrganizationTenantEvidence;

public sealed class OrganizationTenantEvidenceDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid DocumentStorageObjectId { get; set; }
    public required string DocumentDisplayName { get; set; }
    public string? DocumentContentType { get; set; }
    public long DocumentSizeBytes { get; set; }
    public int ReviewStatusId { get; set; }
    public string? ReviewStatusCode { get; set; }
    public string? ReviewStatusName { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid ConcurrencyStamp { get; set; }

    [JsonIgnore]
    public Guid TenantId { get; set; }

    [JsonIgnore]
    public Guid OrganizationTenantId { get; set; }

    [JsonIgnore]
    public Guid? DocumentCreatedBy { get; set; }
}
