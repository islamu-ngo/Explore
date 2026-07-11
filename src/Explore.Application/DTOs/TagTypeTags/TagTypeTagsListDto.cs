using System;

namespace Explore.Application.DTOs.TagTypeTags;

public class TagTypeTagsListDto
{
    public Guid Id { get; set; }
    public Guid TagId { get; set; }
    public string? TagFullName { get; set; }
    public string? TagMasterCode { get; set; } // For i18n with Tolgee
    public int TagTypeId { get; set; }
    public string? TagTypeFullName { get; set; }
    public string? TagTypeMasterCode { get; set; } // For i18n with Tolgee
}
