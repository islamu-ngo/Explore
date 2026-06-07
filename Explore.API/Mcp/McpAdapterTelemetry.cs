// ABOUTME: Emits bounded MCP adapter tracing and metric metadata for local debugging.
// ABOUTME: Keeps prompts, payloads, tenant IDs, auth values, and endpoint details out of telemetry.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Explore.API.Mcp;

public static class McpAdapterTelemetry
{
    public const string ActivitySourceName = "Explore.Mcp";
    public const string MeterName = "Explore.Mcp";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> ToolCalls = Meter.CreateCounter<long>(
        "explore.mcp.tool_calls",
        unit: "{call}",
        description: "Total MCP tool calls by bounded tool name, outcome, and failure code.");
    private static readonly Histogram<double> ToolCallDuration = Meter.CreateHistogram<double>(
        "explore.mcp.tool_call_duration",
        unit: "s",
        description: "MCP tool-call duration by bounded tool name, outcome, and failure code.");

    public static Activity? StartToolCall(string? toolName, bool projected)
    {
        var activity = ActivitySource.StartActivity("mcp.tool.call", ActivityKind.Server);
        if (activity is null)
        {
            return null;
        }

        activity.SetTag("mcp.tool.name", NormalizeToolNameForDiagnostics(toolName));
        activity.SetTag("mcp.tool.projected", projected);
        return activity;
    }

    public static void RecordToolCall(
        TimeSpan duration,
        string? toolName,
        bool projected,
        string? outcome,
        string? failureCode = null)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("tool_name", NormalizeToolNameForDiagnostics(toolName)),
            new("projected", projected),
            new("outcome", NormalizeOutcomeForDiagnostics(outcome)),
            new("failure_code", NormalizeFailureCodeForDiagnostics(failureCode))
        };

        ToolCalls.Add(1, tags);

        if (duration >= TimeSpan.Zero)
        {
            ToolCallDuration.Record(duration.TotalSeconds, tags);
        }
    }

    public static void MarkSuccess(Activity? activity)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("mcp.outcome", "succeeded");
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    public static void MarkFailure(Activity? activity, string? failureCode)
    {
        if (activity is null)
        {
            return;
        }

        var normalizedFailureCode = NormalizeFailureCodeForDiagnostics(failureCode);
        activity.SetTag("mcp.outcome", "failed");
        activity.SetTag("mcp.failure_code", normalizedFailureCode);
        activity.SetStatus(ActivityStatusCode.Error, normalizedFailureCode);
    }

    public static void MarkCancelled(Activity? activity)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("mcp.outcome", "cancelled");
        activity.SetStatus(ActivityStatusCode.Unset);
    }

    public static string NormalizeToolNameForDiagnostics(string? toolName)
        => NormalizeTag(toolName) switch
        {
            "list_ai_tool_contracts" => "list_ai_tool_contracts",
            "propose_ai_tool_action" => "propose_ai_tool_action",
            "propose_create_event_draft" => "propose_create_event_draft",
            _ => "unknown"
        };

    public static string NormalizeOutcomeForDiagnostics(string? outcome)
        => NormalizeTag(outcome) switch
        {
            "succeeded" => "succeeded",
            "failed" => "failed",
            "cancelled" => "cancelled",
            _ => "unknown"
        };

    public static string NormalizeFailureCodeForDiagnostics(string? failureCode)
    {
        var normalized = NormalizeTag(failureCode ?? "none");
        if (normalized == "none")
        {
            return normalized;
        }

        return normalized switch
        {
            "invalid_tool_arguments" => "invalid_tool_arguments",
            "validation_failed" => "validation_failed",
            "not_found" => "not_found",
            "forbidden" => "forbidden",
            "unauthorized" => "unauthorized",
            "quota_exceeded" => "quota_exceeded",
            "cancelled" => "cancelled",
            _ => "unknown"
        };
    }

    private static string NormalizeTag(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? "unknown"
            : value.Trim().ToLowerInvariant();
}
