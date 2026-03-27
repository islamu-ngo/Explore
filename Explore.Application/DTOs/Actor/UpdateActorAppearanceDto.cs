// ABOUTME: DTO for targeted appearance-only updates to an Actor entity.
// ABOUTME: All fields are nullable — only non-null fields are applied (partial update pattern).

using System;

namespace Explore.Application.DTOs.Actor;

/// <summary>
/// Partial update DTO for Actor appearance fields.
/// Send only the fields you want to change; null fields are ignored.
/// Used with the nullable-DTO pattern in UpdateActorCommand.
/// </summary>
public class UpdateActorAppearanceDto
{
    /// <summary>Hex color string (e.g., "#1a2b3c"). Null = no change.</summary>
    public string? BackgroundColor { get; set; }

    /// <summary>Effect name: "SoftOverlay", "StrongOverlay", "Blur", "None". Null = no change.</summary>
    public string? BackgroundEffect { get; set; }

    /// <summary>Hex color string for the banner area. Null = no change.</summary>
    public string? BannerColor { get; set; }

    /// <summary>FK to StorageObject for the banner picture. Null = no change.</summary>
    public Guid? BannerPictureId { get; set; }

    /// <summary>FK to StorageObject for the background image. Null = no change.</summary>
    public Guid? BackgroundImageId { get; set; }
}
