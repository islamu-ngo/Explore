// ABOUTME: Defines the tenant-safe document shape eligible for future AI vector indexing.
// ABOUTME: Limits source text to bounded public event summaries with explicit citation metadata.

namespace Explore.Application.Features.AiAssistant.Rag;

public sealed record AiRagIndexDocument(
    Guid TenantId,
    string Kind,
    Guid ReferenceId,
    AiRagContentScope ContentScope,
    string DisplayName,
    string Summary,
    DateTimeOffset UpdatedAtUtc,
    AiRagCitation Citation);
