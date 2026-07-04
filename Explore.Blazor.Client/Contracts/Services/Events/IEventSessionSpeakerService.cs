// ABOUTME: Client service contract for managing speaker assignments on event sessions.
// ABOUTME: Keeps session speaker UI flows on typed generated API client DTOs.

using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Events;

public interface IEventSessionSpeakerService
{
    Task<HalCollectionResourceOfEventSessionSpeakerListDto> GetSpeakersBySessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<BaseCommandResponseOfGuid?> AddSpeakerToSessionAsync(
        Guid sessionId,
        Guid actorId,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveSpeakerFromSessionAsync(
        Guid sessionId,
        Guid speakerId,
        CancellationToken cancellationToken = default);
}
