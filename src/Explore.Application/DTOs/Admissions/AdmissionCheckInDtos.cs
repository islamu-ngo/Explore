// ABOUTME: Public bounded transport contracts for admission check-in and scanner-capability APIs.
// ABOUTME: Excludes credential digests, ticket identities, persistence descriptors, and attendee PII.

using System.ComponentModel.DataAnnotations;
using Explore.Application.Contracts.Admissions;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Admissions;

public sealed record AdmissionCheckInRequest
{
    public required Guid TargetId { get; init; }

    [Required]
    [StringLength(512, MinimumLength = 1)]
    public required string Credential { get; init; }
}

public sealed record AdmissionScannerCheckInRequest
{
    [Required]
    [StringLength(512, MinimumLength = 1)]
    public required string Credential { get; init; }
}

public sealed record AdmissionCheckInUndoRequest
{
    public required Guid TargetId { get; init; }

    [Required]
    [StringLength(512, MinimumLength = 1)]
    public required string Credential { get; init; }

    [EnumDataType(typeof(AdmissionCheckInUndoReasonCodeEnum))]
    public required AdmissionCheckInUndoReasonCodeEnum ReasonCode { get; init; }
}

public sealed record AdmissionScannerCheckInUndoRequest
{
    [Required]
    [StringLength(512, MinimumLength = 1)]
    public required string Credential { get; init; }

    [EnumDataType(typeof(AdmissionCheckInUndoReasonCodeEnum))]
    public required AdmissionCheckInUndoReasonCodeEnum ReasonCode { get; init; }
}

public sealed record AdmissionCheckInBatchItemRequest
{
    [Required]
    [StringLength(512, MinimumLength = 1)]
    public required string Credential { get; init; }
}

public sealed record AdmissionCheckInBatchRequest
{
    public required Guid TargetId { get; init; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public required IReadOnlyList<AdmissionCheckInBatchItemRequest> Items { get; init; }
}

public sealed record AdmissionScannerCheckInBatchRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public required IReadOnlyList<AdmissionCheckInBatchItemRequest> Items { get; init; }
}

public sealed record AdmissionCheckInResultDto(
    string OutcomeCode,
    Guid TargetId,
    DateTimeOffset? OccurredAt,
    Guid? CheckInId);

public sealed record AdmissionCheckInBatchItemResultDto(
    int Index,
    string OutcomeCode,
    Guid TargetId,
    DateTimeOffset? OccurredAt,
    Guid? CheckInId);

public sealed record AdmissionCheckInBatchResultDto(
    string OutcomeCode,
    IReadOnlyList<AdmissionCheckInBatchItemResultDto> Items);

public sealed record AdmissionCheckInSummaryDto(
    string TargetType,
    long CheckedInCount,
    long UndoneCount,
    long ActiveCount,
    long InactiveCount,
    DateTimeOffset? LastActivityTimeBucketUtc);

public sealed record AdmissionCheckInAuditItemDto(
    string Cursor,
    string Action,
    string Outcome,
    string TargetType,
    DateTimeOffset OccurredAtTimeBucketUtc);

public sealed record AdmissionCheckInAuditPageDto(
    IReadOnlyList<AdmissionCheckInAuditItemDto> Items,
    string? NextCursor);

public sealed record AdmissionCheckInOperationalRequestDto
{
    public required Guid TargetId { get; init; }
    public required AdmissionCheckInOperationalReasonCode ReasonCode { get; init; }
}

public sealed record AdmissionCheckInOperationalResultDto(
    Guid TargetId,
    AdmissionCheckInOperationalAction Action,
    AdmissionCheckInOperationalStatus Status,
    AdmissionCheckInOperationalReasonCode ReasonCode,
    DateTimeOffset OccurredAtUtc);

public sealed record AdmissionCheckInHealthDto(
    Guid TargetId,
    AdmissionCheckInOperationalStatus Status,
    AdmissionCheckInDependencyStatus InfrastructureStatus);

public sealed record IssueAdmissionScannerCapabilityRequest
{
    public required Guid IssueRequestId { get; init; }

    public required Guid TargetId { get; init; }

    [Required]
    [MinLength(1)]
    public required IReadOnlyList<AdmissionCheckInAction> Actions { get; init; }

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public required string DeviceLabel { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }
}

public sealed record RevokeAdmissionScannerCapabilityRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Reason { get; init; }
}

public sealed record AdmissionScannerCapabilityDto(
    Guid ScannerCapabilityId,
    Guid EventId,
    Guid TargetId,
    IReadOnlyList<AdmissionCheckInAction> Actions,
    string DeviceLabel,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    string MaskedCapability);

public sealed record AdmissionScannerCapabilityIssuedDto(
    Guid ScannerCapabilityId,
    Guid EventId,
    Guid TargetId,
    IReadOnlyList<AdmissionCheckInAction> Actions,
    string DeviceLabel,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    string MaskedCapability,
    string? Capability);
