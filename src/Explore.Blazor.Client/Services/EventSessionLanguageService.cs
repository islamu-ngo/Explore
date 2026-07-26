// ABOUTME: Client service for reading and synchronizing event session language assignments.
// ABOUTME: Wraps generated API calls with idempotent diff-based sync for session composers.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Events;
using Explore.Blazor.Client.Helpers;

namespace Explore.Blazor.Client.Services;

public sealed class EventSessionLanguageService : IEventSessionLanguageService
{
    private readonly IEventApiClient _client;

    public EventSessionLanguageService(IEventApiClient client)
    {
        _client = client;
    }

    public async Task<ICollection<EventSessionLanguageListDto>> GetLanguagesBySessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.GetEventSessionLanguagesAsync(sessionId, cancellationToken: cancellationToken);
        return response.GetItems();
    }

    public async Task<bool> SyncLanguagesForSessionAsync(
        Guid sessionId,
        IEnumerable<int> languageIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestedIds = languageIds
                .Where(id => id > 0)
                .ToHashSet();

            var existingAssignments = await GetLanguagesBySessionAsync(sessionId, cancellationToken);
            var existingByLanguageId = existingAssignments
                .Where(assignment => assignment.LanguageId.HasValue && assignment.Id.HasValue)
                .ToDictionary(assignment => assignment.LanguageId!.Value, assignment => assignment.Id!.Value);

            foreach (var languageId in requestedIds.Except(existingByLanguageId.Keys))
            {
                var response = await _client.CreateEventSessionLanguageAsync(new CreateEventSessionLanguageDto
                {
                    EventSessionId = sessionId,
                    LanguageId = languageId
                }, cancellationToken: cancellationToken);

                if (response.Success != true)
                {
                    return false;
                }
            }

            foreach (var assignmentId in existingByLanguageId
                         .Where(pair => !requestedIds.Contains(pair.Key))
                         .Select(pair => pair.Value))
            {
                await _client.DeleteEventSessionLanguageAsync(assignmentId, cancellationToken: cancellationToken);
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ApiException)
        {
            return false;
        }
    }
}
