// ABOUTME: Static configuration for the optional API-hosted MCP adapter.
// ABOUTME: Keeps the adapter disabled by default and documents the selected transport posture.

namespace Explore.API.Configuration;

public sealed class McpAdapterSettings
{
    public const string SectionName = "Mcp";

    public bool Enabled { get; set; }

    public string EndpointPath { get; set; } = "/mcp";

    public bool Stateless { get; set; } = true;

    public bool EnableLegacySse { get; set; }
}
