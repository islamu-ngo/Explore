// ABOUTME: Application-owned versioned configuration records for public home content blocks.
// ABOUTME: Represents persisted configuration documents, not Blazor rendering DTOs.

namespace Explore.Application.Models.PublicExperience;

public sealed record PublicExperienceHomeBlocksConfig(
    int SchemaVersion = 1,
    IReadOnlyList<PublicExperienceHomeBlockConfig>? Blocks = null);

public sealed record PublicExperienceHomeBlockConfig(
    string Id,
    PublicExperienceHomeBlockKind Kind,
    string Title,
    string? Subtitle = null,
    string? Body = null,
    string? ImageUrl = null,
    string? LinkText = null,
    string? LinkUrl = null,
    int SortOrder = 0,
    bool IsEnabled = true);

public enum PublicExperienceHomeBlockKind
{
    Hero = 0,
    RichText = 1,
    EventSection = 2,
    CallToAction = 3,
    OrganizationSummary = 4
}
