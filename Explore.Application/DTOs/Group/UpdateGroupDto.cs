using System;

namespace Explore.Application.DTOs.Group;

public class UpdateGroupDto
{
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public string? MetadataJson { get; set; }
}
