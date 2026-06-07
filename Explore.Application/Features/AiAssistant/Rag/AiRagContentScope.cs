// ABOUTME: Enumerates the only content scopes eligible for future AI RAG indexing.
// ABOUTME: Excludes private/full event content so vector search cannot bypass tenant visibility policy.

namespace Explore.Application.Features.AiAssistant.Rag;

public enum AiRagContentScope
{
    TenantPublicEventSummary = 1,
    GlobalPublicEventSummary = 2,
}
