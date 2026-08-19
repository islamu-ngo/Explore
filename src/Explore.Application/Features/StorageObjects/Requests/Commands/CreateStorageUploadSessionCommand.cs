// ABOUTME: MediatR command for reserving quota and opening a local-first upload session.
// ABOUTME: Uses storage-object create authorization and dynamic tenant/resource attributes.

using Explore.Application.Authorization;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Commands;

[AuthorizeResource(ResourceKinds.StorageObject, AuthorizationActions.Create)]
public class CreateStorageUploadSessionCommand : IRequest<BaseCommandResponse<StorageUploadSessionDto>>, ISecureRequest
{
    public required CreateStorageUploadSessionDto UploadSessionDto { get; set; }
    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => UploadSessionDto.OwningResourceId?.ToString();

    // Upload intent facts are established server-side by CreateStorageUploadSessionAuthorizationContextEnricher,
    // which loads the owning resource. The request declares none: a requested owner is not evidence of one.
}
