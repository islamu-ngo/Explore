// ABOUTME: Defines custom OpenTelemetry business metrics for the platform.
// ABOUTME: Tracks event creation, registration, organization creation, and authorization decisions.

using System.Diagnostics.Metrics;

namespace Explore.Application.Telemetry;

/// <summary>
/// Custom business metrics exposed via OpenTelemetry.
/// All counters include dimensional tags (tenant_id, resource_type) for multi-tenant analytics.
/// Meter name: "Explore.Business"
/// </summary>
public sealed class BusinessMetrics
{
    public const string MeterName = "Explore.Business";

    private readonly Counter<long> _eventsCreated;
    private readonly Counter<long> _eventsPublished;
    private readonly Counter<long> _registrationsCreated;
    private readonly Counter<long> _organizationsCreated;
    private readonly Counter<long> _authorizationDecisions;

    public BusinessMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _eventsCreated = meter.CreateCounter<long>(
            "explore.events.created",
            unit: "{event}",
            description: "Total number of events created");

        _eventsPublished = meter.CreateCounter<long>(
            "explore.events.published",
            unit: "{event}",
            description: "Total number of events published");

        _registrationsCreated = meter.CreateCounter<long>(
            "explore.registrations.created",
            unit: "{registration}",
            description: "Total number of event registrations created");

        _organizationsCreated = meter.CreateCounter<long>(
            "explore.organizations.created",
            unit: "{organization}",
            description: "Total number of organizations created");

        _authorizationDecisions = meter.CreateCounter<long>(
            "explore.authorization.decisions",
            unit: "{decision}",
            description: "Total authorization decisions (allowed/denied)");
    }

    public void RecordEventCreated(string? tenantId = null, string? eventType = null)
    {
        _eventsCreated.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"),
            new KeyValuePair<string, object?>("event_type", eventType ?? "unknown"));
    }

    public void RecordEventPublished(string? tenantId = null)
    {
        _eventsPublished.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"));
    }

    public void RecordRegistrationCreated(string? tenantId = null)
    {
        _registrationsCreated.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"));
    }

    public void RecordOrganizationCreated(string? tenantId = null)
    {
        _organizationsCreated.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId ?? "default"));
    }

    public void RecordAuthorizationDecision(string resource, string action, bool allowed)
    {
        _authorizationDecisions.Add(1,
            new KeyValuePair<string, object?>("resource", resource),
            new KeyValuePair<string, object?>("action", action),
            new KeyValuePair<string, object?>("result", allowed ? "allowed" : "denied"));
    }
}
