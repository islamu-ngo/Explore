// ABOUTME: Defines same-origin BFF reads and HAL-gated ticket-transfer lifecycle mutations.
// ABOUTME: Keeps claim capabilities and one-time credentials out of URLs and diagnostic contracts.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Admissions;

public interface ITicketTransferService
{
    Task<HalResourceOfTicketTransferDto?> GetAsync(
        Guid eventId,
        Guid admissionTicketId,
        Guid transferId,
        string? capability,
        CancellationToken cancellationToken);

    Task<TicketTransferOfferResponse?> OfferAsync(
        Guid eventId,
        Guid admissionTicketId,
        CancellationToken cancellationToken);

    Task<TicketTransferCredentialResponse?> AcceptAsync(
        Guid eventId,
        Guid admissionTicketId,
        Guid transferId,
        Guid recipientParticipantId,
        string? capability,
        CancellationToken cancellationToken);

    Task<HalResourceOfTicketTransferDto?> CancelAsync(
        Guid eventId,
        Guid admissionTicketId,
        Guid transferId,
        CancellationToken cancellationToken);

    Task<TicketTransferCredentialResponse?> CorrectAsync(
        Guid eventId,
        Guid admissionTicketId,
        Guid transferId,
        CancellationToken cancellationToken);

    Task<TicketTransferCredentialResponse?> ReissueAsync(
        Guid eventId,
        Guid admissionTicketId,
        Guid transferId,
        CancellationToken cancellationToken);
}
