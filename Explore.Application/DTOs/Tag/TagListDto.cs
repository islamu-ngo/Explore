using System;

namespace Explore.Application.DTOs.Tag;

public class TagListDto
{
    public Guid Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
}
