// ABOUTME: Requests governed PDS snapshot reconciliation under the current global Jetstream lease.
// ABOUTME: Carries only bounded configuration, a fenced claim, and an optional completed-work fingerprint.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Federation.Atproto.Models;
using MediatR;

namespace Explore.Application.Features.Federation.Atproto.Requests.Commands;

public sealed record ReconcileAtprotoPdsSnapshotsCommand(
    AtprotoJetstreamClaim Claim,
    IReadOnlyCollection<string> AllowedDids,
    DateTime SnapshotStartedAt,
    string? LastCompletedFingerprint = null) : IRequest<AtprotoPdsRecoveryResult>;
