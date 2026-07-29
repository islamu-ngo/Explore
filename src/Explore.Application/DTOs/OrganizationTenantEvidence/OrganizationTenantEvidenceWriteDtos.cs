// ABOUTME: Write contracts for attaching and reviewing OrganizationTenant legitimacy evidence.
// ABOUTME: Accepts only an application storage identifier plus a bounded review decision and notes.

namespace Explore.Application.DTOs.OrganizationTenantEvidence;

public sealed class CreateOrganizationTenantEvidenceUploadSessionDto
{
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long ExpectedSizeBytes { get; set; }
}

public sealed class SubmitOrganizationTenantEvidenceDto
{
    public Guid DocumentStorageObjectId { get; set; }
}

public sealed class ReviewOrganizationTenantEvidenceDto
{
    public OrganizationTenantEvidenceReviewDecisionDto Decision { get; set; }
    public string? Notes { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
}

public enum OrganizationTenantEvidenceReviewDecisionDto
{
    Approve = 1,
    Reject = 2
}
