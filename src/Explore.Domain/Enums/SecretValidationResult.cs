// ABOUTME: Result of the most recent validation attempt against a SecretBinding's data plane.
// ABOUTME: Validation fetches (and discards) the value to prove the source resolves.

namespace Explore.Domain.Enums;

public enum SecretValidationResult
{
    NotValidated = 0,
    Success = 1,
    Failure = 2,
}
