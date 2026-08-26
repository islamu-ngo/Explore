// ABOUTME: Defines the closed PII-free reason vocabulary for compensating admission check-in facts.
// ABOUTME: Prevents operator prose from entering append-only admission history, logs, or exports.

namespace Explore.Domain.Enums;

public enum AdmissionCheckInUndoReasonCodeEnum
{
    OperatorCorrection = 1,
    DuplicateScan = 2,
    WrongTarget = 3,
    ExceptionalReconciliation = 4
}
