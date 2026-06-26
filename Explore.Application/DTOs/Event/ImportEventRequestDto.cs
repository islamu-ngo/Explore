// ABOUTME: Request DTO for importing an event from an external source or backfill.
// ABOUTME: Requires provenance metadata so the imported event can be traced to its origin.

namespace Explore.Application.DTOs.Event;

public sealed class ImportEventRequestDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required Guid TenantId { get; set; }
    public required Guid OwnerActorId { get; set; }
    public required string ProvenanceSource { get; set; }
    public required string ProvenanceExternalId { get; set; }
    public int? EventTypeId { get; set; }
    public int? AudienceGenderId { get; set; }
    public int? AudienceAgeId { get; set; }
    public int? VisibilityTypeId { get; set; }
    public int? EventFormatId { get; set; }
    public string? Timezone { get; set; }
    public decimal? Price { get; set; }
    public bool IsRegistrationRequired { get; set; }
    public Guid? FeaturedImageId { get; set; }
}
