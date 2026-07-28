// ABOUTME: Grouped PATCH contract for tenant moderation reporting routing settings.
// ABOUTME: Provider credentials are explicit nested writes and remain absent from read models.

using Explore.Application.Features.EventReporting.Models;
using Explore.Application.Settings.Groups;

namespace Explore.Application.DTOs.EventReporting;

public sealed class UpdateReportingRoutingSettingsDto
{
    public ReportingRoutingPolicyUpdateDto? Policy { get; set; }
    public ReportingProviderRoutingUpdateDto? Osprey { get; set; }
    public ReportingProviderRoutingUpdateDto? Coop { get; set; }
}

public sealed class ReportingRoutingPolicyUpdateDto
{
    public bool ExternalSyncEnabled { get; set; }
    public EventReportProviderEvidenceMode EvidenceMode { get; set; } = EventReportProviderEvidenceMode.MetadataOnly;
}

public sealed class ReportingProviderRoutingUpdateDto
{
    public bool Enabled { get; set; }
    public string RoutingMode { get; set; } = ReportingRoutingMode.Both;
    public string? EndpointUrl { get; set; }
    public ReportingProviderCredentialsUpdateDto? Credentials { get; set; }
}

public sealed class ReportingProviderCredentialsUpdateDto
{
    public string? ApiKey { get; set; }
    public string? WebhookSecret { get; set; }
}
