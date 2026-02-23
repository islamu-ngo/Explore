using Explore.Application.Authorization;
using Explore.Application.DTOs.IndexedDid;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.IndexedDids.Requests.Commands;

[AuthorizeResource("indexed_did", PermissionAction.Create)]
public class CreateIndexedDidCommand : IRequest<BaseCommandResponse<string>>, ISecureRequest
{
    public required CreateIndexedDidDto IndexedDidDto { get; set; }

    string? ISecureRequest.ResourceId => null;
}
