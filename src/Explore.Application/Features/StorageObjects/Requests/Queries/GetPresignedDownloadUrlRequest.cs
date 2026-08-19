// ABOUTME: MediatR query for fetching a pre-signed download URL by storage object ID.
// ABOUTME: Returns the signed URL string.
using Explore.Application.Authorization;
using Explore.Application.DTOs.StorageObject;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Queries;

/// <summary>
/// Query to get a presigned download URL for a storage object by its ID.
/// </summary>
[AuthorizeResource(ResourceKinds.StorageObject, AuthorizationActions.StorageObjects.PresignedDownload)]
public class GetPresignedDownloadUrlRequest : IRequest<PresignedDownloadUrlResponseDto?>, ISecureRequest
{
    /// <summary>
    /// The ID of the storage object.
    /// </summary>
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    /// <summary>
    /// The expiration time in minutes for the presigned URL. Default is 60.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    string? ISecureRequest.ResourceId => Id == Guid.Empty ? null : Id.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        TenantId == Guid.Empty
        ? null
        : new StorageObjectCollectionAuthorizationFacts(TenantId);
}
