// ABOUTME: Handles AI reference search by mapping tenant-filtered event entities into safe DTOs.
// ABOUTME: Enforces bounded result counts and prevents full event content from entering AI reference output.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Ai;
using Explore.Application.Features.AiAssistant.Requests.Queries;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Handlers.Queries;

public sealed class SearchAiReferencesQueryHandler(IEventRepository eventRepository)
    : IRequestHandler<SearchAiReferencesQuery, IReadOnlyList<AiReferenceSearchResultDto>>
{
    public const int DefaultLimit = 10;
    public const int MaxLimit = 20;
    private const int MinimumSearchTermLength = 2;
    private const int MaxSummaryLength = 240;
    private const string EventReferenceKind = "Event";

    public async Task<IReadOnlyList<AiReferenceSearchResultDto>> Handle(
        SearchAiReferencesQuery request,
        CancellationToken cancellationToken)
    {
        string searchTerm = request.SearchTerm.Trim();

        if (searchTerm.Length < MinimumSearchTermLength)
        {
            return [];
        }

        int limit = NormalizeLimit(request.Limit);
        IReadOnlyList<Event> events = await eventRepository.SearchAiReferenceEventsAsync(
            searchTerm,
            limit,
            cancellationToken);

        return events.Select(MapEvent).ToList();
    }

    private static int NormalizeLimit(int limit)
    {
        if (limit <= 0)
        {
            return DefaultLimit;
        }

        return Math.Min(limit, MaxLimit);
    }

    private static AiReferenceSearchResultDto MapEvent(Event @event)
    {
        return new AiReferenceSearchResultDto(
            EventReferenceKind,
            @event.Id,
            @event.Title,
            BuildSummary(@event),
            @event.FirstSessionDate,
            @event.LastSessionDate,
            @event.EventStatus?.FullName,
            @event.VisibilityType?.FullName,
            @event.EventFormat?.FullName);
    }

    private static string? BuildSummary(Event @event)
    {
        string? summary = FirstNonBlank(@event.Subtitle, @event.Description);
        return summary is null ? null : Truncate(summary, MaxSummaryLength);
    }

    private static string? FirstNonBlank(params string?[] values)
    {
        foreach (string? value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength].TrimEnd();
    }
}
