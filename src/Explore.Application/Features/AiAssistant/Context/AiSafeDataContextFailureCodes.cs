// ABOUTME: Defines stable failure codes for AI schema-only data context validation.
// ABOUTME: Keeps UI, diagnostics, and future adapters aligned without exposing rejected field names.

namespace Explore.Application.Features.AiAssistant.Context;

public static class AiSafeDataContextFailureCodes
{
    public const string ContextKindNotAllowed = "context_kind_not_allowed";
    public const string ContextFieldNotAllowed = "context_field_not_allowed";
}
