// ABOUTME: Stages optional ATProto thumbnail bytes before fenced database materialization.
// ABOUTME: Exposes exact provider results so failed or deduplicated imports can clean staged objects.

using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Application.Models.Storage;

namespace Explore.Application.Contracts.Infrastructure;

public interface IAtprotoThumbnailBlobGateway
{
    Task<FileStorageWriteResult?> FetchAndStageAsync(
        AtprotoThumbnailBlobCandidate? candidate,
        Guid tenantId,
        CancellationToken cancellationToken);

    Task CleanupAsync(
        FileStorageWriteResult staged,
        CancellationToken cancellationToken);
}
