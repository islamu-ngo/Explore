// ABOUTME: Category detail DTO returned by category read endpoints.
// ABOUTME: Includes concurrency metadata required by PATCH If-Match updates.

using System;

namespace Explore.Application.DTOs.Category;

public class CategoryDto
{
    public Guid Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public Guid? ParentId { get; set; }
    public string? ParentFullName { get; set; }
    public Guid TenantId { get; set; }
}
