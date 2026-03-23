// ABOUTME: Typed policy for instance branding — display name, logo, favicon, and custom CSS.
// ABOUTME: Each field is a PolicySlot allowing instance admins to lock tenant branding overrides.

namespace Explore.Domain.Policies;

public sealed class BrandingPolicy
{
    public PolicySlot<string> DisplayName { get; set; } = new(string.Empty);
    public PolicySlot<string> LogoUrl { get; set; } = new(string.Empty);
    public PolicySlot<string> FaviconUrl { get; set; } = new(string.Empty);
    public PolicySlot<string> CustomCssUrl { get; set; } = new(string.Empty);
}
