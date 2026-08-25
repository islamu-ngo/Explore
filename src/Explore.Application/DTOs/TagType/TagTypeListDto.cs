namespace Explore.Application.DTOs.TagType;

public sealed record TagTypeListDto
{
    public int Id { get; init; }
    public required string MasterCode { get; init; } // For i18n with Tolgee
    public required string FullName { get; init; } // Fallback default
}
