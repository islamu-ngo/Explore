// ABOUTME: Category detail DTO returned by category read endpoints.
// ABOUTME: Includes concurrency metadata required by PATCH If-Match updates.

using System;

namespace Explore.Application.DTOs.Category;

public sealed record CategoryDto
{
    public Guid Id { get; init; }
    public Guid ConcurrencyStamp { get; init; }
    public required string MasterCode { get; init; }
    public required string FullName { get; init; }
    public Guid? ParentId { get; init; }
    public string? ParentFullName { get; init; }
    public Guid TenantId { get; init; }
}
