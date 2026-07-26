// ABOUTME: Input contract for creating or replacing an event public action destination.
// ABOUTME: Carries semantic action metadata while server code owns review health state.

namespace Explore.Application.DTOs.EventPublicAction;

public sealed class ManageEventPublicActionDto
{
    public int KindId { get; set; }
    public required string Url { get; set; }
    public string? Label { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
}
