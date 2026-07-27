// ABOUTME: Describes one validated tenant-local Event and EventSession import from a canonical ATProto record.
// ABOUTME: Preserves source identity and mapped optional calendar fields for atomic persistence.

namespace Explore.Application.Features.Federation.Atproto.Models;

using Explore.Application.Models.Storage;
using Explore.Application.DTOs.Event;

public sealed record AtprotoFederatedEventImportPlan(
    Guid TenantId,
    Guid AtprotoRecordId,
    string Did,
    string AtUri,
    string Name,
    DateTimeOffset CreatedAt,
    string? Description,
    string? SourceUrl,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string? Mode,
    string? Status,
    bool? RsvpExpected)
{
    public string TimeZoneId { get; init; } = "UTC";
    public required ConfigureEventParticipationDto ParticipationConfiguration { get; init; }
    public AtprotoThumbnailBlobCandidate? Thumbnail { get; init; }
    public FileStorageWriteResult? StagedThumbnail { get; init; }
}
