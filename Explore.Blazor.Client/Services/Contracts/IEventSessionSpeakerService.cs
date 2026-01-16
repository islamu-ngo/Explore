using Explore.Blazor.Client.Clients;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Explore.Blazor.Client.Services.Contracts;

public interface IEventSessionSpeakerService
{
    Task<ICollection<EventSessionSpeakerListDto>> GetSpeakersBySessionAsync(Guid sessionId);
    Task<BaseCommandResponseOfGuid> AddSpeakerToSessionAsync(CreateEventSessionSpeakerDto speaker);
    Task RemoveSpeakerFromSessionAsync(Guid speakerId);
}
