namespace Explore.Application.DTOs.DidCustodyType;

public sealed record DidCustodyTypeDto
{
    public int Id { get; init; }
    public required string MasterCode { get; init; } // For i18n with Tolgee
    public required string FullName { get; init; } // Fallback default
    public string? Description { get; init; }
}
