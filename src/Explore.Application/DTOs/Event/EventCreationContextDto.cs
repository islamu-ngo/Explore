// ABOUTME: Event creation context returned by the API before users start composing a draft.
// ABOUTME: Centralizes tenant publishing policy and publisher affordances for the Blazor create flow.

namespace Explore.Application.DTOs.Event;

public sealed record EventCreationContextDto
{
    public bool CanCreate { get; init; }

    public bool AllowPersonalPublishing { get; init; }

    public bool AllowOrganizationPublishing { get; init; }

    public bool AllowGroupPublishing { get; init; }

    public bool RequiresApproval { get; init; }

    public string? DefaultPublisherMode { get; init; }

    public string? UnavailableReason { get; init; }

    public List<EventCreationPublisherOptionDto> PublisherOptions { get; init; } = [];
}
