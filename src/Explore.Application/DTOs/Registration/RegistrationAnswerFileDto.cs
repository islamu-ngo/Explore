// ABOUTME: Safe administrative representation of registration-file quarantine and release audit state.
// ABOUTME: Exposes no provider object key, raw URI, checksum, or downloadable credential.

namespace Explore.Application.DTOs.Registration;

public sealed record RegistrationAnswerFileDto(
    Guid Id,
    Guid RegistrationSubmissionId,
    Guid RegistrationFormFieldId,
    Guid StorageObjectId,
    string SafeDisplayName,
    string ContentType,
    string Extension,
    long Size,
    string QuarantineState,
    string ScanStatus,
    DateTime QuarantinedAt,
    Guid? ReleasedBy,
    DateTime? ReleasedAt,
    string? ReleaseReason);

public sealed record RegistrationAnswerFileReleaseInputDto(string Reason);
