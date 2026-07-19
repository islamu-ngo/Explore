// ABOUTME: Defines bounded, credential-free outcomes for ATProto OAuth session revocation.
// ABOUTME: Distinguishes idempotent absence and remote failure while guaranteeing local clearance.

namespace Explore.Application.Features.Authentication.Atproto.Models;

public enum AtprotoSessionRevocationOutcome
{
    Revoked,
    AlreadyAbsent,
    RemoteFailedLocalCleared
}

public sealed record AtprotoSessionRevocationResult(AtprotoSessionRevocationOutcome Outcome);
