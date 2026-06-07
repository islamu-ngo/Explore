// ABOUTME: Carries safe citation metadata for future AI RAG search results.
// ABOUTME: Provides linkable reference identity without exposing private event content or provider data.

namespace Explore.Application.Features.AiAssistant.Rag;

public sealed record AiRagCitation(
    string Label,
    string RouteName,
    string ResourcePath);
