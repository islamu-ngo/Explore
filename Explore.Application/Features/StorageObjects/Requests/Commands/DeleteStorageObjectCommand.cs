// ABOUTME: MediatR command for deleting a storage object by ID.
// ABOUTME: Carries the target storage object ID.
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Commands;

[AuthorizeResource("storage_object", AuthorizationActions.Delete)]
public class DeleteStorageObjectCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
