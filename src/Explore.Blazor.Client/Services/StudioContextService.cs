// ABOUTME: Scoped generated-client adapter for private Studio context and event order collections.
// ABOUTME: Keeps purchaser PII and guest capabilities outside Studio order reads.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;

namespace Explore.Blazor.Client.Services;

public sealed class StudioContextService(IEventApiClient apiClient) : IStudioContextService
{
    public async Task<HalResourceOfStudioContextDto?> GetContextAsync(
        Guid? actorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await apiClient.GetStudioContextAsync(actorId, cancellationToken: cancellationToken);
        }
        catch (ApiException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<HalResourceOfRegistrationOrderDto>> GetEventOrdersAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var resource = await apiClient.GetEventRegistrationOrdersAsync(eventId, cancellationToken: cancellationToken);
        return resource._embedded?.Items?.ToArray() ?? [];
    }
}
