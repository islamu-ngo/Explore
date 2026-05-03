// ABOUTME: Event creation context returned by the API before users start composing a draft.
// ABOUTME: Centralizes tenant publishing policy and publisher affordances for the Blazor create flow.

namespace Explore.Application.DTOs.Event;

public class EventCreationContextDto
{
    public bool CanCreate { get; set; }

    public bool AllowPersonalPublishing { get; set; }

    public bool AllowOrganizationPublishing { get; set; }

    public bool AllowGroupPublishing { get; set; }

    public bool RequiresApproval { get; set; }

    public string? DefaultPublisherMode { get; set; }

    public string? UnavailableReason { get; set; }

    public List<EventCreationPublisherOptionDto> PublisherOptions { get; set; } = [];
}
