// ABOUTME: Defines the append-only fact actions in an admission check-in ledger.
// ABOUTME: Undo compensates for a check-in without deleting or rewriting its history.

namespace Explore.Domain.Enums;

public enum AdmissionCheckInActionEnum
{
    CheckIn = 1,
    Undo = 2
}
