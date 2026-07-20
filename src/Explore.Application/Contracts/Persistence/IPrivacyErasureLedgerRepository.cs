// ABOUTME: Defines append-only access to the application database's PII-free erasure ledger.
// ABOUTME: Supports locally allocated facts and exact retained-fact mirroring without update or delete operations.

using Explore.Application.Contracts.PrivacyErasure;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IPrivacyErasureLedgerRepository
{
    Task<PrivacyErasureIntent> AppendAsync(
        PrivacyErasureRequest intent,
        CancellationToken cancellationToken);

    Task<PrivacyErasureIntent> AppendAsync(
        PrivacyErasureIntent intent,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PrivacyErasureIntent>> ReadAfterAsync(
        long authoritySequence,
        int limit,
        CancellationToken cancellationToken);
}
