// ABOUTME: Centralizes safe model-facing correction instructions for rejected AI tool payloads.
// ABOUTME: Keeps retry prompts consistent and free of raw rejected arguments or validation internals.

namespace Explore.Application.Features.AiAssistant.Tools;

public static class AiToolCorrectionMessages
{
    public const string SchemaExactRetry = "Regenerate the tool call arguments as a JSON object that matches the registered schema exactly. Include required fields, use the documented JSON types and formats, and omit unsupported, forbidden, tenant, status, audit, or execution fields.";
}
