using System;

// ABOUTME: Wrapper DTO for partial tag updates using nullable property groups.
// ABOUTME: Body IDs and tenant IDs are absent because PATCH routes use route/context authority.

namespace Explore.Application.DTOs.Tag;

public sealed record UpdateTagDto
{
    public UpdateTagMasterCodeDto? MasterCode { get; init; }
    public UpdateTagFullNameDto? FullName { get; init; }
}

public sealed record UpdateTagMasterCodeDto
{
    public required string Value { get; init; }
}

public sealed record UpdateTagFullNameDto
{
    public required string Value { get; init; }
}
