using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.IndexedDids.Requests.Commands;

[AuthorizeResource("indexed_did", PermissionAction.Delete)]
public class DeleteIndexedDidCommand : IRequest<bool>, ISecureRequest
{
    public required string Did { get; set; }

    string? ISecureRequest.ResourceId => Did;
}
