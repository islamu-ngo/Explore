using System;

// ABOUTME: Wrapper DTO for partial tag updates using nullable property groups.
// ABOUTME: Body IDs and tenant IDs are absent because PATCH routes use route/context authority.

namespace Explore.Application.DTOs.Tag;

public class UpdateTagDto
{
    public UpdateTagMasterCodeDto? MasterCode { get; set; }
    public UpdateTagFullNameDto? FullName { get; set; }
}

public class UpdateTagMasterCodeDto
{
    public required string Value { get; set; }
}

public class UpdateTagFullNameDto
{
    public required string Value { get; set; }
}
