// ABOUTME: MediatR command that streams bytes into the selected provider and finalizes a reserved upload session.
// ABOUTME: Keeps browser/API upload transport provider-neutral while preserving storage-object authorization metadata.

using Explore.Application.Authorization;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Commands;

[AuthorizeResource(ResourceKinds.StorageObject, AuthorizationActions.Create)]
public class FinalizeStorageUploadSessionCommand : IRequest<BaseCommandResponse<StorageUploadSessionDto>>, ISecureRequest
{
    public Guid UploadSessionId { get; set; }
    public required Stream Content { get; set; }
    public string? ContentType { get; set; }
    public long? ContentLength { get; set; }
    public Guid? TenantId { get; set; }

    string? ISecureRequest.ResourceId => UploadSessionId == Guid.Empty ? null : UploadSessionId.ToString();

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new StorageObjectCollectionAuthorizationFacts(TenantId ?? Guid.Empty);
}
