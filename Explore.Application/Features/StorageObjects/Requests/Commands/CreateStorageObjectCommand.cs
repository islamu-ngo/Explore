using Explore.Application.Authorization;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Commands;

[AuthorizeResource("storage_object", PermissionAction.Create)]
public class CreateStorageObjectCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateStorageObjectDto StorageObjectDto { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        StorageObjectDto.TenantId != Guid.Empty
            ? new Dictionary<string, object> { ["tenantId"] = StorageObjectDto.TenantId.ToString() }
            : null;
}
