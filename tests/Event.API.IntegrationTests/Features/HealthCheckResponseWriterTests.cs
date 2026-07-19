// ABOUTME: Verifies shared health endpoint JSON keeps operator diagnostics bounded and secret-free.
// ABOUTME: Covers the ServiceDefaults writer used by both /health readiness and /alive liveness endpoints.

using System.Text;
using System.Text.Json.Nodes;
using Event.Api.IntegrationTests.Fixtures;
using Explore.ServiceDefaults.HealthChecks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

[Category(TestCategories.Fast)]
public sealed class HealthCheckResponseWriterTests
{
    [Test]
    public async Task WriteAsync_WhenEntryHasException_RedactsRawFailureDetails()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var data = new Dictionary<string, object>
        {
            ["provider"] = "s3_compatible",
            ["status"] = "unhealthy",
            ["failureCode"] = "provider_unreachable",
            ["endpoint"] = "https://s3.example.test/private",
            ["bucketName"] = "private-bucket",
            ["objectKey"] = "tenant-a/private/file.png",
            ["localPath"] = "/srv/islamu-event/private/file.png",
            ["apiKeyConfigured"] = true,
            ["durationMs"] = 12
        };

        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["storage"] = new(
                    HealthStatus.Unhealthy,
                    "Provider failed at https://s3.example.test/private",
                    TimeSpan.FromMilliseconds(12),
                    new InvalidOperationException("Failed to probe https://s3.example.test/private with secret-token from /srv/islamu-event/private/file.png"),
                    data)
            },
            TimeSpan.FromMilliseconds(12));

        await HealthCheckResponseWriter.WriteAsync(context, report);

        var json = ReadResponseJson(context);
        var root = JsonNode.Parse(json)!.AsObject();
        var check = root["checks"]!.AsArray()[0]!.AsObject();
        var checkData = check["data"]!.AsObject();

        check["error"]!.GetValue<string>().Should().Be(HealthCheckResponseWriter.RedactedErrorMessage);
        check["description"].Should().BeNull();
        checkData["provider"]!.GetValue<string>().Should().Be("s3_compatible");
        checkData["status"]!.GetValue<string>().Should().Be("unhealthy");
        checkData["failureCode"]!.GetValue<string>().Should().Be("provider_unreachable");
        checkData["apiKeyConfigured"]!.GetValue<bool>().Should().BeTrue();
        checkData["durationMs"]!.GetValue<int>().Should().Be(12);
        checkData["endpoint"]!.GetValue<string>().Should().Be(HealthCheckResponseWriter.RedactedValue);
        checkData["bucketName"]!.GetValue<string>().Should().Be(HealthCheckResponseWriter.RedactedValue);
        checkData["objectKey"]!.GetValue<string>().Should().Be(HealthCheckResponseWriter.RedactedValue);
        checkData["localPath"]!.GetValue<string>().Should().Be(HealthCheckResponseWriter.RedactedValue);

        json.Should().NotContain("https://s3.example.test");
        json.Should().NotContain("secret-token");
        json.Should().NotContain("private-bucket");
        json.Should().NotContain("/srv/islamu-event");
        json.Should().NotContain("tenant-a/private/file.png");
    }

    [Test]
    public async Task WriteAsync_WhenEntryIsHealthy_PreservesDocumentedShapeAndSafeData()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["mcp-adapter"] = new(
                    HealthStatus.Healthy,
                    "MCP adapter posture is bounded.",
                    TimeSpan.FromMilliseconds(3),
                    exception: null,
                    data: new Dictionary<string, object>
                    {
                        ["enabled"] = true,
                        ["startupEnabled"] = true,
                        ["runtimeEnabled"] = true,
                        ["legacySseRuntimeEnabled"] = false
                    })
            },
            TimeSpan.FromMilliseconds(3));

        await HealthCheckResponseWriter.WriteAsync(context, report);

        var json = ReadResponseJson(context);
        var root = JsonNode.Parse(json)!.AsObject();
        var check = root["checks"]!.AsArray()[0]!.AsObject();
        var checkData = check["data"]!.AsObject();

        context.Response.ContentType.Should().Be("application/json; charset=utf-8");
        context.Response.Headers["X-Health-Status"].ToString().Should().Be("Healthy");
        context.Response.Headers["Cache-Control"].ToString().Should().Be("no-cache, no-store, must-revalidate");
        root["status"]!.GetValue<string>().Should().Be("Healthy");
        root["message"]!.GetValue<string>().Should().Be("Ok");
        check["name"]!.GetValue<string>().Should().Be("mcp-adapter");
        check["status"]!.GetValue<string>().Should().Be("Healthy");
        check["description"]!.GetValue<string>().Should().Be("MCP adapter posture is bounded.");
        check["error"].Should().BeNull();
        checkData["enabled"]!.GetValue<bool>().Should().BeTrue();
        checkData["startupEnabled"]!.GetValue<bool>().Should().BeTrue();
        checkData["runtimeEnabled"]!.GetValue<bool>().Should().BeTrue();
        checkData["legacySseRuntimeEnabled"]!.GetValue<bool>().Should().BeFalse();
    }

    [Test]
    public async Task WriteAsync_WhenSensitiveKeysContainPrimitiveValues_RedactsBeforeTypeInspection()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["email-dispatch"] = new(
                    HealthStatus.Healthy,
                    description: null,
                    TimeSpan.Zero,
                    exception: null,
                    data: new Dictionary<string, object>
                    {
                        ["tenantId"] = 42,
                        ["userId"] = 17L,
                        ["providerId"] = 3,
                        ["recipientAddress"] = "person@example.test",
                        ["pendingCount"] = 9
                    })
            },
            TimeSpan.Zero);

        await HealthCheckResponseWriter.WriteAsync(context, report);

        var root = JsonNode.Parse(ReadResponseJson(context))!.AsObject();
        var checkData = root["checks"]!.AsArray()[0]!["data"]!.AsObject();
        checkData["tenantId"]!.GetValue<string>().Should().Be(HealthCheckResponseWriter.RedactedValue);
        checkData["userId"]!.GetValue<string>().Should().Be(HealthCheckResponseWriter.RedactedValue);
        checkData["providerId"]!.GetValue<string>().Should().Be(HealthCheckResponseWriter.RedactedValue);
        checkData["recipientAddress"]!.GetValue<string>().Should().Be(HealthCheckResponseWriter.RedactedValue);
        checkData["pendingCount"]!.GetValue<int>().Should().Be(9);
    }

    private static string ReadResponseJson(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        return reader.ReadToEnd();
    }
}
