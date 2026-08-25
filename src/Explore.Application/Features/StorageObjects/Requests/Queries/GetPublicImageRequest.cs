// ABOUTME: Query request to retrieve a public image by storage object ID.
// ABOUTME: Used by the OG image proxy endpoint for stable, non-expiring image URLs.

using Explore.Application.Models.Storage;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Queries;

public sealed record GetPublicImageRequest(Guid StorageObjectId) : IRequest<StorageObjectContentResult?>;
