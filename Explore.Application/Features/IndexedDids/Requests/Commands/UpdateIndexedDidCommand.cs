// ABOUTME: MediatR command for updating an indexed DID record.
// ABOUTME: Carries the UpdateIndexedDidDto payload.
using Explore.Application.Authorization;
using Explore.Application.DTOs.IndexedDid;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.IndexedDids.Requests.Commands;

[AuthorizeResource("indexed_did", PermissionAction.Update)]
public class UpdateIndexedDidCommand : IRequest<BaseCommandResponse<string>>, ISecureRequest
{
    public required UpdateIndexedDidDto IndexedDidDto { get; set; }

    string? ISecureRequest.ResourceId => IndexedDidDto.Did;
}
