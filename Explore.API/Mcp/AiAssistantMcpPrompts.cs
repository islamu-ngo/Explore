// ABOUTME: MCP prompt templates for external agents using the governed AI tool registry.
// ABOUTME: Teaches proposal-first workflows without exposing tenant, provider, prompt, or secret data.

using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace Explore.API.Mcp;

[McpServerPromptType]
public sealed class AiAssistantMcpPrompts
{
    [McpServerPrompt(
        Name = "create_event_draft_with_confirmation",
        Title = "Create event draft with confirmation")]
    [Authorize(Policy = McpAuthorizationPolicies.Propose)]
    [Description("Guide an external agent through proposing an event draft while preserving ISLAMU Event confirmation and authorization boundaries.")]
    public string CreateEventDraftWithConfirmation()
        => """
           You are operating against the ISLAMU Event MCP adapter.

           Required workflow:
           1. Call list_ai_tool_contracts and use the CreateEventDraft schema plus projected McpToolName exactly.
           2. Prefer the projected propose_create_event_draft MCP tool when available, passing the target conversation id plus allowed registry payload fields. Use propose_ai_tool_action only as the generic fallback.
           3. Treat the returned proposed action id as pending. Do not claim an event was created.
           4. wait for the user or product UI to confirm the proposed action through the normal API/HAL confirmation flow.

           Safety constraints:
           - Do not include tenant values, provider URLs, API keys, model secrets, raw provider responses, or prompt transcripts.
           - Do not attempt direct repository mutation or event creation.
           - If payload validation fails, ask the user for missing safe fields instead of guessing privileged fields.
           """;
}
