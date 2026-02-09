using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services.Contracts;

public interface IEventSessionSpeakerService
{
    Task<ICollection<object>> GetSpeakersBySessionAsync(Guid sessionId);
    Task<BaseCommandResponseOfGuid?> AddSpeakerToSessionAsync(object speaker);
    Task<bool> RemoveSpeakerFromSessionAsync(Guid speakerId);
}
