// ABOUTME: Lightweight sitemap event entry DTO for SEO XML generation.
// ABOUTME: Keeps sitemap projection outside controllers while preserving entity-first repositories.

namespace Explore.Application.DTOs.Seo;

public sealed record SitemapEventEntryDto(Guid EventId, DateTime LastModifiedAt);
