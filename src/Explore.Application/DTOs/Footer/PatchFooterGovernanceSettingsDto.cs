// ABOUTME: Presence-aware write DTO for instance footer-governance lock settings.
// ABOUTME: Distinguishes omitted flags from explicit lock changes without using the read DTO as a command body.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.Footer;

public sealed class PatchFooterGovernanceSettingsDto
{
    public OptionalUpdate<bool> LockTenantTemplate { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantLinkGroups { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantSocialLinks { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantDescription { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantCopyright { get; set; } = OptionalUpdate<bool>.Unspecified();

    public bool HasChanges() => LockTenantTemplate.HasValue || LockTenantLinkGroups.HasValue
        || LockTenantSocialLinks.HasValue || LockTenantDescription.HasValue || LockTenantCopyright.HasValue;
}
