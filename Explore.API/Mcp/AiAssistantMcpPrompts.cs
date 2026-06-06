// ABOUTME: MCP prompt templates for external agents using the governed AI tool registry.
// ABOUTME: Teaches proposal-first workflows without exposing tenant, provider, prompt, or secret data.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Explore.API.Mcp;

[McpServerPromptType]
public sealed class AiAssistantMcpPrompts
{
    [McpServerPrompt(
        Name = "create_event_draft_with_confirmation",
        Title = "Create event draft with confirmation")]
    [Description("Guide an external agent through proposing an event draft while preserving ISLAMU Event confirmation and authorization boundaries.")]
    public string CreateEventDraftWithConfirmation()
        => """
           You are operating against the ISLAMU Event MCP adapter.

           Required workflow:
           1. Call list_ai_tool_contracts and use the CreateEventDraft schema exactly.
           2. Call propose_ai_tool_action with the target conversation id, tool name CreateEventDraft, and a payload containing only allowed fields.
           3. Treat the returned proposed action id as pending. Do not claim an event was created.
           4. wait for the user or product UI to confirm the proposed action through the normal API/HAL confirmation flow.

           Safety constraints:
           - Do not include tenant values, provider URLs, API keys, model secrets, raw provider responses, or prompt transcripts.
           - Do not attempt direct repository mutation or event creation.
           - If payload validation fails, ask the user for missing safe fields instead of guessing privileged fields.
           """;
}
