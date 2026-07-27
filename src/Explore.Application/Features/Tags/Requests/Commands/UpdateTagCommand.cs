// ABOUTME: MediatR command for updating an existing tag.
// ABOUTME: Carries the UpdateTagDto payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Tag;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Tags.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tag, AuthorizationActions.Update)]
public class UpdateTagCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TagId { get; set; }
    public Guid TenantId { get; set; }
    public required UpdateTagDto Update { get; set; }

    string? ISecureRequest.ResourceId => TagId.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        TenantId != Guid.Empty
            ? new Dictionary<string, object> { ["tenantId"] = TenantId.ToString() }
            : null;
}
