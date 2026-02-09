using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Contracts;

namespace Explore.Blazor.Client.Services;

public class EventSessionSpeakerService : IEventSessionSpeakerService
{
    private readonly IEventApiClient _client;

    public EventSessionSpeakerService(IEventApiClient client)
    {
        _client = client;
    }

    public Task<ICollection<object>> GetSpeakersBySessionAsync(Guid sessionId)
    {
        // TODO: Fix this when API client is regenerated.
        return Task.FromResult<ICollection<object>>(new List<object>());
    }

    public Task<BaseCommandResponseOfGuid?> AddSpeakerToSessionAsync(object speaker)
    {
        // TODO: Fix this when API client is regenerated.
        return Task.FromResult<BaseCommandResponseOfGuid?>(null);
    }

    public Task<bool> RemoveSpeakerFromSessionAsync(Guid speakerId)
    {
        // TODO: Fix this when API client is regenerated. EventSessionSpeakerDELETEAsync doesn't exist.
        return Task.FromResult(false);
    }
}
