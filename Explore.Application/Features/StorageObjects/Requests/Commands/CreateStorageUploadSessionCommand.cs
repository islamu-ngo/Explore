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

    IDictionary<string, object>? ISecureRequest.ResourceAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>();

            if (TenantId != Guid.Empty)
            {
                attributes["tenantId"] = TenantId.ToString();
            }

            if (!string.IsNullOrWhiteSpace(UploadSessionDto.OwningResourceKind))
            {
                attributes["owningResourceKind"] = UploadSessionDto.OwningResourceKind;
            }

            if (UploadSessionDto.OwningResourceId.HasValue)
            {
                attributes["owningResourceId"] = UploadSessionDto.OwningResourceId.Value.ToString();
            }

            attributes["purpose"] = UploadSessionDto.Purpose;
            attributes["visibility"] = UploadSessionDto.Visibility;

            return attributes.Count == 0 ? null : attributes;
        }
    }
}
