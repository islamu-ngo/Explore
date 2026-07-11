// ABOUTME: Builds durable Listmonk subscriber sync outbox rows from registration consent inputs.
// ABOUTME: Keeps registration handlers free of Listmonk HTTP calls while preserving tenant setting gates.

using Explore.Application.DTOs.EventRegistration;
using Explore.Domain;

namespace Explore.Application.Contracts.Services;

public interface IListmonkRegistrationSyncOutboxFactory
{
    Task<IntegrationSyncOutbox?> CreateForRegistrationAsync(
        Event eventEntity,
        User user,
        CreateEventRegistrationDto dto,
        Guid registrationIntentId,
        CancellationToken cancellationToken);
}
