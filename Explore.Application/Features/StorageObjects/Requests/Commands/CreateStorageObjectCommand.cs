// ABOUTME: MediatR command for creating a new storage object record.
// ABOUTME: Carries the CreateStorageObjectDto payload.
using Explore.Application.Authorization;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Commands;

[AuthorizeResource(ResourceKinds.StorageObject, AuthorizationActions.Create)]
public class CreateStorageObjectCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateStorageObjectDto StorageObjectDto { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        StorageObjectDto.TenantId != Guid.Empty
            ? new Dictionary<string, object> { ["tenantId"] = StorageObjectDto.TenantId.ToString() }
            : null;
}
