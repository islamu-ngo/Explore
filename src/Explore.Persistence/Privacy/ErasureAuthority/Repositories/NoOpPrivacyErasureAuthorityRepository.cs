// ABOUTME: Implements a no-op privacy-erasure authority for Topology=None deployments.
// ABOUTME: Returns monotonic in-memory sequences without persisting authority state to SQLite or PostgreSQL.

using Explore.Application.Configuration;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Domain;
using Microsoft.Extensions.Options;

namespace Explore.Persistence.Privacy.ErasureAuthority.Repositories;

public sealed class NoOpPrivacyErasureAuthorityRepository(
    TimeProvider? timeProvider = null,
    IOptions<PrivacyErasureOptions>? options = null) : IPrivacyErasureAuthority
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly IOptions<PrivacyErasureOptions>? _options = options;
    private long _sequence;

    public Task<PrivacyErasureIntent> AppendAsync(
        PrivacyErasureRequest intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        long sequence = Interlocked.Increment(ref _sequence);
        DateTime recordedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        TimeSpan retention = _options?.Value.AuthorityRetention ?? TimeSpan.FromDays(365);
        var created = PrivacyErasureIntent.Record(
            intent.IntentId,
            sequence,
            intent.SubjectKind,
            intent.SubjectId,
            intent.ReasonCode,
            intent.PolicyVersion,
            recordedAtUtc,
            recordedAtUtc,
            recordedAtUtc + retention);

        return Task.FromResult(created);
    }

    public Task<IReadOnlyList<PrivacyErasureIntent>> ReadAfterAsync(
        long authoritySequence,
        int limit,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PrivacyErasureIntent> result = Array.Empty<PrivacyErasureIntent>();
        return Task.FromResult(result);
    }
}
