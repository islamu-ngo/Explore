// ABOUTME: Typed client boundary for account and capability-scoped registration order reads and lifecycle actions.
// ABOUTME: Keeps generated API calls, guest headers, and active-actor order aggregation outside Razor components.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services;

public interface IRegistrationOrderService
{
    Task<RegistrationCheckoutCompositionDto?> GetCheckoutAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<GuestRegistrationOrderStartDto?> StartGuestAsync(Guid eventId, StartRegistrationOrderRequest request, CancellationToken cancellationToken = default);
    Task<BaseCommandResponseOfGuid?> StartAuthenticatedAsync(Guid eventId, StartRegistrationOrderRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HalResourceOfRegistrationOrderDto>> GetActorOrdersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HalResourceOfRegistrationOrderDto>> GetEventOrdersAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationOrderDto?> GetCurrentAsync(Guid eventId, Guid orderId, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationOrderDto?> ContinueCurrentAsync(Guid eventId, Guid orderId, int? contributionBasisPoints, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationOrderDto?> FinalizeCurrentAsync(Guid eventId, Guid orderId, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationOrderDto?> CancelCurrentAsync(Guid eventId, Guid orderId, CancellationToken cancellationToken = default);
    Task<HalResourceOfGuestRegistrationOrderDto?> GetGuestAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, CancellationToken cancellationToken = default);
    Task<GuestRegistrationOrderLifecycleResponse?> CancelGuestAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, CancellationToken cancellationToken = default);
    Task<GuestRegistrationOrderLifecycleResponse?> ContinueGuestAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, int? contributionBasisPoints, CancellationToken cancellationToken = default);
    Task<GuestRegistrationOrderLifecycleResponse?> FinalizeGuestAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, CancellationToken cancellationToken = default);
}

public sealed record GuestRegistrationOrderCapability
{
    internal GuestRegistrationOrderCapability(string value) => Value = value;

    internal string Value { get; }
}
