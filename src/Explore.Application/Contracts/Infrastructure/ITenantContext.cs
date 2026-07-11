namespace Explore.Application.Contracts.Infrastructure;

/// <summary>
/// Provides access to the current tenant context.
/// In single-tenant mode, this returns the default tenant ID.
/// In multi-tenant mode, this would extract the tenant from the request.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant ID from the request context.
    /// </summary>
    Guid TenantId { get; }
}
