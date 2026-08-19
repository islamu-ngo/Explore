// ABOUTME: MediatR command for canceling an upload session and releasing reserved quota.
// ABOUTME: Uses storage-object delete authorization because cancellation removes the pending upload affordance.

using Explore.Application.Authorization;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Commands;

[AuthorizeResource(ResourceKinds.StorageObject, AuthorizationActions.Delete)]
public class CancelStorageUploadSessionCommand : IRequest<BaseCommandResponse<StorageUploadSessionDto>>, ISecureRequest
{
    public Guid UploadSessionId { get; set; }
    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => UploadSessionId.ToString();

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new StorageObjectCollectionAuthorizationFacts(TenantId);
}
