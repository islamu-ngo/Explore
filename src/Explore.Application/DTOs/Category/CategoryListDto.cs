// ABOUTME: Category list item DTO used by lookup, filter, and HAL collection responses.
// ABOUTME: Includes concurrency metadata so list-driven editors can issue PATCH If-Match updates.

using System;

namespace Explore.Application.DTOs.Category;

public class CategoryListDto
{
    public Guid Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public Guid? ParentId { get; set; }
    public string? ParentFullName { get; set; }
}
