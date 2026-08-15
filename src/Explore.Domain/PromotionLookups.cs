// ABOUTME: Defines normalized lookup rows for promotion definition and reservation statuses.
// ABOUTME: Keeps persisted status IDs tied to stable master codes without exposing enum columns.

namespace Explore.Domain;

public sealed class PromotionDefinitionStatus
{
    public int Id { get; set; }
    public string MasterCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class PromotionReservationStatus
{
    public int Id { get; set; }
    public string MasterCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
