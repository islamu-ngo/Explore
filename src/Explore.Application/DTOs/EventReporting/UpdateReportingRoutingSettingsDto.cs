// ABOUTME: Write DTO for tenant moderation reporting provider routing settings.
// ABOUTME: Accepts optional provider secrets for persistence without exposing them in read models.

using Explore.Application.Features.EventReporting.Models;
using Explore.Application.Settings.Groups;

namespace Explore.Application.DTOs.EventReporting;

public sealed class UpdateReportingRoutingSettingsDto
{
    public bool ExternalSyncEnabled { get; init; } = true;

    public bool EnableTenantOspreyProvider { get; init; }

    public bool EnableTenantCoopProvider { get; init; }

    public string OspreyRoutingMode { get; init; } = ReportingRoutingMode.Both;

    public string CoopRoutingMode { get; init; } = ReportingRoutingMode.Both;

    public EventReportProviderEvidenceMode EvidenceMode { get; init; } = EventReportProviderEvidenceMode.MetadataOnly;

    public string? OspreyEndpointUrl { get; init; }

    public string? OspreyApiKey { get; init; }

    public string? OspreyWebhookSecret { get; init; }

    public string? CoopEndpointUrl { get; init; }

    public string? CoopApiKey { get; init; }

    public string? CoopWebhookSecret { get; init; }
}
