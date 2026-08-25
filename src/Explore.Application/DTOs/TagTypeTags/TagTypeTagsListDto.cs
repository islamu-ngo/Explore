using System;

namespace Explore.Application.DTOs.TagTypeTags;

public sealed record TagTypeTagsListDto
{
    public Guid Id { get; init; }
    public Guid TagId { get; init; }
    public string? TagFullName { get; init; }
    public string? TagMasterCode { get; init; } // For i18n with Tolgee
    public int TagTypeId { get; init; }
    public string? TagTypeFullName { get; init; }
    public string? TagTypeMasterCode { get; init; } // For i18n with Tolgee
}
