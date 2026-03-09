// ABOUTME: Centralizes trusted tenant-related HTTP header names shared between hosts.
// ABOUTME: Keeps BFF forwarding and API resolution aligned on the same header contract.

namespace Explore.Application.Constants;

public static class TenantHeaderNames
{
    public const string TenantId = "X-Tenant-Id";

    public const string TenantSlug = "X-Tenant-Slug";
}
