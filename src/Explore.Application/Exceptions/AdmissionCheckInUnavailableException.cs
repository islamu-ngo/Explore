// ABOUTME: Signals that online admission authority or persistence could not produce a safe decision.
// ABOUTME: Exposes no credential, digest, ticket, participant, scanner-capability, or provider details.

namespace Explore.Application.Exceptions;

public sealed class AdmissionCheckInUnavailableException() :
    Exception("Admission check-in is temporarily unavailable.");
