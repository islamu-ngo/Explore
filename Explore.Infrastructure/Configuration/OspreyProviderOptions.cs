// ABOUTME: Static configuration for the Osprey moderation signal provider adapter.
// ABOUTME: Keeps endpoint credentials and transport choices inside Infrastructure only.

namespace Explore.Infrastructure.Configuration;

public sealed class OspreyProviderOptions
{
    public const string SectionName = "Reporting:Osprey";

    public bool Enabled { get; set; }
    public string EndpointUrl { get; set; } = string.Empty;
    public string EvaluatePath { get; set; } = "/api/v1/evaluate";
    public string ApiKey { get; set; } = string.Empty;
    public string ApiKeyHeaderName { get; set; } = "Authorization";
    public string EventType { get; set; } = "event_report";
    public int TimeoutSeconds { get; set; } = 10;
    public bool AllowLocalProviderEndpoints { get; set; }
}
