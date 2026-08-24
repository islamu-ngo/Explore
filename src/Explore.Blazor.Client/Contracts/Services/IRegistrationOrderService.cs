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
    Task<HalResourceOfRegistrationOrderDto?> ApplyCurrentPromotionAsync(Guid eventId, Guid orderId, HalResourceOfRegistrationOrderDto order, string code, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationOrderDto?> RemoveCurrentPromotionAsync(Guid eventId, Guid orderId, HalResourceOfRegistrationOrderDto order, CancellationToken cancellationToken = default);
    Task<PaidOrderAcceptanceDisclosureDto?> GetCurrentPaymentAcceptanceAsync(Guid eventId, Guid orderId, HalResourceOfRegistrationOrderDto order, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationPaymentDto?> StartCurrentPaymentAsync(Guid eventId, Guid orderId, HalResourceOfRegistrationOrderDto order, string disclosureRevision, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationPaymentDto?> GetCurrentPaymentAsync(Guid eventId, Guid orderId, HalResourceOfRegistrationOrderDto order, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationPaymentDto?> RefreshCurrentPaymentAsync(Guid eventId, Guid orderId, HalResourceOfRegistrationPaymentDto payment, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationPaymentDto?> RetryCurrentPaymentAsync(Guid eventId, Guid orderId, HalResourceOfRegistrationPaymentDto payment, CancellationToken cancellationToken = default);
    Task<string?> IssueCurrentPaymentCheckoutTicketAsync(string path, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationOrderParticipantsDto?> GetCurrentParticipantsAsync(Guid eventId, Guid orderId, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationOrderParticipantsDto?> SaveCurrentParticipantAsync(Guid eventId, Guid orderId, Guid? participantId, Guid lineId, int ordinal, RegistrationParticipantRequest request, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationOrderParticipantsDto?> DeferCurrentParticipantsAsync(Guid eventId, Guid orderId, IReadOnlyCollection<TicketDeferralInputDto> assignments, DateTimeOffset deadline, CancellationToken cancellationToken = default);
    Task<HalResourceOfGuestRegistrationOrderDto?> GetGuestAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationOrderParticipantsDto?> GetGuestParticipantsAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationOrderParticipantsDto?> SaveGuestParticipantAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, Guid? participantId, Guid lineId, int ordinal, RegistrationParticipantRequest request, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationOrderParticipantsDto?> DeferGuestParticipantsAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, IReadOnlyCollection<TicketDeferralInputDto> assignments, DateTimeOffset deadline, CancellationToken cancellationToken = default);
    Task<GuestRegistrationOrderLifecycleResponseDto?> CancelGuestAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, CancellationToken cancellationToken = default);
    Task<GuestRegistrationOrderLifecycleResponseDto?> ContinueGuestAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, int? contributionBasisPoints, CancellationToken cancellationToken = default);
    Task<GuestRegistrationOrderLifecycleResponseDto?> FinalizeGuestAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, CancellationToken cancellationToken = default);
    Task<HalResourceOfGuestRegistrationOrderDto?> ApplyGuestPromotionAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, HalResourceOfGuestRegistrationOrderDto order, string code, CancellationToken cancellationToken = default);
    Task<HalResourceOfGuestRegistrationOrderDto?> RemoveGuestPromotionAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, HalResourceOfGuestRegistrationOrderDto order, CancellationToken cancellationToken = default);
    Task<PaidOrderAcceptanceDisclosureDto?> GetGuestPaymentAcceptanceAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, HalResourceOfGuestRegistrationOrderDto order, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationPaymentDto?> StartGuestPaymentAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, HalResourceOfGuestRegistrationOrderDto order, string disclosureRevision, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationPaymentDto?> GetGuestPaymentAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, HalResourceOfGuestRegistrationOrderDto order, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationPaymentDto?> RefreshGuestPaymentAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, HalResourceOfRegistrationPaymentDto payment, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationPaymentDto?> RetryGuestPaymentAsync(Guid eventId, Guid orderId, GuestRegistrationOrderCapability capability, HalResourceOfRegistrationPaymentDto payment, CancellationToken cancellationToken = default);
    Task<string?> IssueGuestPaymentCheckoutTicketAsync(string path, GuestRegistrationOrderCapability capability, CancellationToken cancellationToken = default);
    Task<HalResourceOfRegistrationPaymentDto?> GetStudioPaymentAsync(Guid eventId, Guid orderId, HalResourceOfRegistrationOrderDto order, CancellationToken cancellationToken = default);
}
