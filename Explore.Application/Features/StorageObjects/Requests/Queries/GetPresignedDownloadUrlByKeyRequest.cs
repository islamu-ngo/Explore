// ABOUTME: MediatR query for fetching a pre-signed download URL by storage key.
// ABOUTME: Returns the signed URL string.
using Explore.Application.DTOs.StorageObject;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Queries;

/// <summary>
/// Query to get a presigned download URL using an object key directly.
/// </summary>
public class GetPresignedDownloadUrlByKeyRequest : IRequest<PresignedDownloadUrlResponseDto?>
{
    /// <summary>
    /// The object key (path) in S3-compatible storage.
    /// </summary>
    public required string ObjectKey { get; set; } = string.Empty;

    /// <summary>
    /// The expiration time in minutes for the presigned URL. Default is 60.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;
}
