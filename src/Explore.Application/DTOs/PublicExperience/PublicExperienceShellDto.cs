// ABOUTME: Typed public-experience shell read model for anonymous tenant-local public UI composition.
// ABOUTME: Keeps organization-centric posture in Application DTOs rather than Domain entities or UI settings blobs.

using Explore.Application.DTOs.Footer;
using Explore.Application.Models;
using Explore.Application.Models.PublicExperience;

namespace Explore.Application.DTOs.PublicExperience;

public class PublicExperienceShellDto
{
    public int SchemaVersion { get; set; } = 1;
    public string Revision { get; set; } = string.Empty;
    public PublicExperienceMode Mode { get; set; } = PublicExperienceMode.DiscoveryCentric;
    public string RailPublicVisibility { get; set; } = "AuthenticatedOnly";
    public PublicExperienceHomeDto Home { get; set; } = new();
    public PublicExperienceNavigationDto Navigation { get; set; } = new();
    public PublicExperienceEventCatalogDto EventCatalog { get; set; } = new();
    public PublicExperiencePrimaryOrganizationDto PrimaryOrganization { get; set; } = new();
    public List<PublicExperienceEventSectionDto> EventSections { get; set; } = new();
    public List<PublicExperienceCtaDto> Ctas { get; set; } = new();
    public FooterConfigDto Footer { get; set; } = new();
}

public class PublicExperienceHomeDto
{
    public string PreferredHomePage { get; set; } = "EventList";
    public string BrandDisplayName { get; set; } = string.Empty;
    public string BrandLogoUrl { get; set; } = string.Empty;
    public string BrandFaviconUrl { get; set; } = string.Empty;
    public List<PublicExperienceHomeBlockDto> Blocks { get; set; } = new();
}

public class PublicExperienceHomeBlockDto
{
    public string Key { get; set; } = string.Empty;
    public PublicExperienceHomeBlockKind Kind { get; set; } = PublicExperienceHomeBlockKind.RichText;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string LinkText { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class PublicExperienceNavigationDto
{
    public List<PublicExperienceNavigationLinkDto> Links { get; set; } = new();
}

public class PublicExperienceNavigationLinkDto
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class PublicExperienceEventCatalogDto
{
    public string Label { get; set; } = "Events";
    public string Url { get; set; } = "/events";
}

public class PublicExperiencePrimaryOrganizationDto
{
    public PublicExperiencePrimaryOrganizationState State { get; set; } = PublicExperiencePrimaryOrganizationState.NotConfigured;
    public Guid? OrganizationId { get; set; }
    public Guid? ActorId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Handle { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public string ProfilePictureUri { get; set; } = string.Empty;
}

public class PublicExperienceEventSectionDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = "/events";
    public string Icon { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class PublicExperienceCtaDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public PublicExperienceCtaPlacement Placement { get; set; }
    public PublicExperienceCtaStyle Style { get; set; } = PublicExperienceCtaStyle.Primary;
    public int SortOrder { get; set; }
}
