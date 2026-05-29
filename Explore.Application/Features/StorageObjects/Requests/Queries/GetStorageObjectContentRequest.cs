// ABOUTME: MediatR query for streaming storage object content by stable metadata ID.
// ABOUTME: Avoids exposing provider object keys or filesystem paths as browser-facing identifiers.

using Explore.Application.Models.Storage;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Queries;

public sealed class GetStorageObjectContentRequest : IRequest<StorageObjectContentResult?>
{
    public Guid StorageObjectId { get; set; }
}
