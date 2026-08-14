// ABOUTME: Safe Application result for server-authoritative paid catalog publication readiness.
// ABOUTME: Returns stable blocker codes and bounded explanations without provider secrets or account identifiers.

namespace Explore.Application.DTOs.EventTicketing;

public sealed class PaidEventPublicationPreflightDto
{
    public Guid EventId { get; init; }
    public Guid? CatalogId { get; init; }
    public bool IsPaidCatalog { get; init; }
    public bool IsReady { get; init; }
    public IReadOnlyList<PaidEventPublicationPreflightBlockerDto> Blockers { get; init; } = [];
}

public sealed class PaidEventPublicationPreflightBlockerDto
{
    public string Code { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
}
