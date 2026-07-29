// ABOUTME: Repository contract for tenant-visible AT Protocol discovery and exact outbound ownership lookup.
// ABOUTME: Keeps globally canonical records hidden unless a tenant presentation or ownership row authorizes access.

using Explore.Domain;
using Explore.Domain.Federation;

namespace Explore.Application.Contracts.Persistence;

public interface IAtprotoRecordRepository
{
    Task<AtprotoRecord?> GetById(Guid id);
    Task<bool> Exists(Guid id);
    Task<List<AtprotoRecord>> GetAllAtprotoRecords();
    Task<AtprotoRecord?> GetAtprotoRecordByUri(string uri);
    Task<List<AtprotoRecord>> GetAtprotoRecordsByDid(string did);
    Task<List<AtprotoRecord>> GetAtprotoRecordsByCollection(string collection);
    Task<AtprotoRecord?> GetOwnedRecordAsync(
        Guid tenantId,
        Guid userId,
        string sourceEntityType,
        Guid sourceEntityId,
        CancellationToken cancellationToken = default);

    Task<AtprotoOutboundRecordOwnership?> GetOwnedRecordForSourceAsync(
        Guid tenantId,
        string sourceEntityType,
        Guid sourceEntityId,
        CancellationToken cancellationToken = default);

    Task<AtprotoOutboundRecordOwnership?> GetOwnedRsvpForUserEventAsync(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        string sourceEntityType,
        string collection,
        CancellationToken cancellationToken = default);

    Task<List<AtprotoOutboundRecordOwnership>> GetLiveGroundedEventOwnershipsForActorAsync(
        Guid actorId,
        CancellationToken cancellationToken = default);

    Task<List<AtprotoOutboundRecordOwnership>> GetLiveGroundedEventOwnershipsForActorAndDidAsync(
        Guid actorId,
        string did,
        CancellationToken cancellationToken = default);
}
