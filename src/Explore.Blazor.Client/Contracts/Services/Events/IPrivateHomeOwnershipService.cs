// ABOUTME: Client contract for consent-backed private home classification and ownership acceptance.
// ABOUTME: Carries the consent version so the acknowledgement the user saw is auditable server-side.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Events;

public interface IPrivateHomeOwnershipService
{
    Task<BaseCommandResponseOfGuid> ClassifyAsPrivateHomeAsync(
        Guid locationId,
        Guid expectedConcurrencyStamp,
        PrivateHomeOwnershipConsentDto consent,
        CancellationToken cancellationToken = default);

    Task<BaseCommandResponseOfGuid> AcceptOwnershipAsync(
        Guid locationId,
        Guid expectedConcurrencyStamp,
        PrivateHomeOwnershipConsentDto consent,
        CancellationToken cancellationToken = default);
}
