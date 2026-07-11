// ABOUTME: Scope at which a SecretBinding is bound.
// ABOUTME: Instance = platform-wide; Tenant = per-tenant override.

namespace Explore.Domain.Enums;

public enum SecretScope
{
    Instance = 0,
    Tenant = 1,
}
