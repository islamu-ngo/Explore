// ABOUTME: MediatR command for removing an indexed DID by ID.
// ABOUTME: Carries the target DID record ID.
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.IndexedDids.Requests.Commands;

[AuthorizeResource("indexed_did", AuthorizationActions.Delete)]
public class DeleteIndexedDidCommand : IRequest<bool>, ISecureRequest
{
    public required string Did { get; set; }

    string? ISecureRequest.ResourceId => Did;
}
