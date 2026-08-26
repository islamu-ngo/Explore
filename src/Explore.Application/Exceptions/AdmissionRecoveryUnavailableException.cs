// ABOUTME: Signals that a public recovery request could not be staged durably.
// ABOUTME: Carries no identity or capability material into diagnostics or HTTP responses.

namespace Explore.Application.Exceptions;

public sealed class AdmissionRecoveryUnavailableException() :
    Exception("Admission recovery is temporarily unavailable.");
