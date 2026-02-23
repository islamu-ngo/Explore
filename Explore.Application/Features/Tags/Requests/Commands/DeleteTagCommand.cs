using System;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.Tags.Requests.Commands;

[AuthorizeResource("tag", PermissionAction.Delete)]
public class DeleteTagCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
