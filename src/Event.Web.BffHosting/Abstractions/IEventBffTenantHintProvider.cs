// ABOUTME: Defines the shared BFF adapter contract for trusted tenant route hints.
// ABOUTME: Prevents browser-supplied tenant headers from becoming downstream authority.

namespace Event.Web.BffHosting.Abstractions;

public interface IEventBffTenantHintProvider
{
    string? ResolveTenantSlug(HttpContext httpContext);
}
