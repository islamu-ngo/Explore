// ABOUTME: MediatR command for updating storage object metadata.
// ABOUTME: Carries the UpdateStorageObjectDto payload.
using Explore.Application.Authorization;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Commands;

[AuthorizeResource(ResourceKinds.StorageObject, AuthorizationActions.Update)]
public class UpdateStorageObjectCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateStorageObjectDto StorageObjectDto { get; set; }

    string? ISecureRequest.ResourceId => StorageObjectDto.Id.ToString();
}
