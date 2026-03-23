// ABOUTME: MediatR query for fetching a pre-signed download URL by storage object ID.
// ABOUTME: Returns the signed URL string.
using Explore.Application.DTOs.StorageObject;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Queries;

/// <summary>
/// Query to get a presigned download URL for a storage object by its ID.
/// </summary>
public class GetPresignedDownloadUrlRequest : IRequest<PresignedDownloadUrlResponseDto?>
{
    /// <summary>
    /// The ID of the storage object.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The expiration time in minutes for the presigned URL. Default is 60.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;
}
