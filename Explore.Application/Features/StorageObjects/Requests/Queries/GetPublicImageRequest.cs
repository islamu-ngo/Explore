// ABOUTME: Query request to retrieve a public image by storage object ID.
// ABOUTME: Used by the OG image proxy endpoint for stable, non-expiring image URLs.

using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Queries;

public class GetPublicImageRequest : IRequest<(Stream FileStream, string ContentType)?>
{
    public Guid StorageObjectId { get; set; }
}
