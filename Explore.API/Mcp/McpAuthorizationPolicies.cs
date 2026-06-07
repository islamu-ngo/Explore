// ABOUTME: Authorization policy names for the API-hosted MCP adapter surface.
// ABOUTME: Keeps API-key scope requirements explicit while allowing normal authenticated user sessions.

namespace Explore.API.Mcp;

public static class McpAuthorizationPolicies
{
    public const string Read = "mcp_read";
    public const string Propose = "mcp_propose";
}
