// ABOUTME: Input contract for creating or replacing an event public action destination.
// ABOUTME: Carries semantic action metadata while server code owns review health state.

namespace Explore.Application.DTOs.EventPublicAction;

public sealed record ManageEventPublicActionDto
{
    public int KindId { get; init; }
    public required string Url { get; init; }
    public string? Label { get; init; }
    public int SortOrder { get; init; }
    public bool IsPrimary { get; init; }
    public Guid ExpectedConcurrencyStamp { get; init; }
}
