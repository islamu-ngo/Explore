namespace Explore.Application.DTOs.Event;

using System.Collections.Immutable;

public sealed record EventPublishReadinessDto
{
    public Guid EventId { get; init; }
    public bool IsReady { get; init; }
    private IReadOnlyList<EventPublishReadinessErrorDto>? _errors = ImmutableArray<EventPublishReadinessErrorDto>.Empty;

    public IReadOnlyList<EventPublishReadinessErrorDto> Errors
    {
        get => _errors!;
        init => _errors = value?.ToImmutableArray();
    }
}
