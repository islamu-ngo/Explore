// ABOUTME: Defines account and one-time recovery admission ticket delivery documents.
// ABOUTME: Marks bearer-bearing fields explicitly while keeping recovery wrappers link-free.

namespace Explore.Application.DTOs.AdmissionTickets;

public sealed record AdmissionTicketDto(
    Guid Id,
    Guid TicketId,
    Guid EventId,
    string StatusCode,
    string DisplayReference);

public sealed record AdmissionTicketRecoveryRequestResult(bool Accepted, bool Success);

public sealed record AdmissionTicketRecoveryDeliveryDto(
    Guid Id,
    Guid TicketId,
    Guid EventId,
    string StatusCode,
    string DisplayReference,
    string ManualCode,
    string ManualCodeClassificationCode,
    string QrRepresentation,
    string PrintModel);

public sealed record AdmissionTicketRecoveryConsumeResult(
    Guid RecoveryRecordId,
    AdmissionTicketRecoveryDeliveryDto Delivery);

public sealed record AdmissionTicketQrDeliveryDto(
    Guid Id,
    Guid TicketId,
    Guid EventId,
    string StatusCode,
    string DisplayReference,
    string ManualCode,
    string ManualCodeClassificationCode,
    string QrRepresentation,
    string PrintModel,
    string DeliverySurface);

public sealed record AdmissionTicketPrintDeliveryDto(
    Guid Id,
    Guid TicketId,
    Guid EventId,
    string StatusCode,
    string DisplayReference,
    string ManualCode,
    string ManualCodeClassificationCode,
    string QrRepresentation,
    string PrintModel,
    string DeliverySurface);
