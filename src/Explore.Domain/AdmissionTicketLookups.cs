// ABOUTME: Normalized lookup rows for admission ticket, credential, and transition-reason identities.
// ABOUTME: Keeps stable persisted IDs and codes separate from Domain enum convenience mirrors.

namespace Explore.Domain;

public sealed class AdmissionTicketStatus
{
    public int Id { get; set; }
    public string MasterCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class AdmissionTicketCredentialStatus
{
    public int Id { get; set; }
    public string MasterCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class AdmissionTicketTransitionReason
{
    public int Id { get; set; }
    public string MasterCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
