// ABOUTME: Normalized lookup rows for contact-share consent subject identity shapes.
// ABOUTME: Keeps subject type IDs stable for uniqueness, history, and export audit snapshots.

namespace Explore.Domain;

public sealed class ContactShareConsentSubjectType
{
    public int Id { get; set; }
    public string MasterCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
