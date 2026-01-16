using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Explore.Blazor.Client.Services;

public class EventSessionSpeakerService : IEventSessionSpeakerService
{
    private readonly IEventApiClient _client;

    public EventSessionSpeakerService(IEventApiClient client)
    {
        _client = client;
    }

    public async Task<ICollection<EventSessionSpeakerListDto>> GetSpeakersBySessionAsync(Guid sessionId)
    {
        return await _client.BySession4Async(sessionId);
    }

    public async Task<BaseCommandResponseOfGuid> AddSpeakerToSessionAsync(CreateEventSessionSpeakerDto speaker)
    {
        return await _client.EventSessionSpeakerPOSTAsync(speaker);
    }

    public async Task RemoveSpeakerFromSessionAsync(Guid speakerId)
    {
        await _client.EventSessionSpeakerDELETEAsync(speakerId);
    }
}
