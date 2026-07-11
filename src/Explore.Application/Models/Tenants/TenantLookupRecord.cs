// ABOUTME: Represents the tenant lookup values needed by runtime tenant-resolution caches.
// ABOUTME: Keeps cache-loading data shape in Application so Infrastructure can stay persistence-agnostic.

namespace Explore.Application.Models.Tenants;

public sealed class TenantLookupRecord
{
    public Guid TenantId { get; set; }

    public required string Slug { get; set; }

    public string? Subdomain { get; set; }

    public string? CustomDomain { get; set; }
}
