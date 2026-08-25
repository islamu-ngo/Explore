// ABOUTME: Event creation context returned by the API before users start composing a draft.
// ABOUTME: Centralizes tenant publishing policy and publisher affordances for the Blazor create flow.

namespace Explore.Application.DTOs.Event;

using System.Collections.Immutable;

public sealed record EventCreationContextDto
{
    public bool CanCreate { get; init; }

    public bool AllowPersonalPublishing { get; init; }

    public bool AllowOrganizationPublishing { get; init; }

    public bool AllowGroupPublishing { get; init; }

    public bool RequiresApproval { get; init; }

    public string? DefaultPublisherMode { get; init; }

    public string? UnavailableReason { get; init; }

    private IReadOnlyList<EventCreationPublisherOptionDto>? _publisherOptions = ImmutableArray<EventCreationPublisherOptionDto>.Empty;

    public IReadOnlyList<EventCreationPublisherOptionDto> PublisherOptions
    {
        get => _publisherOptions!;
        init => _publisherOptions = value?.ToImmutableArray();
    }
}
