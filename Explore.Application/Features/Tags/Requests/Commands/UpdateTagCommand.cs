// ABOUTME: MediatR command for updating an existing tag.
// ABOUTME: Carries the UpdateTagDto payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Tag;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Tags.Requests.Commands;

[AuthorizeResource("tag", PermissionAction.Update)]
public class UpdateTagCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateTagDto TagDto { get; set; }

    string? ISecureRequest.ResourceId => TagDto.Id.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        TagDto.TenantId != Guid.Empty
            ? new Dictionary<string, object> { ["tenantId"] = TagDto.TenantId.ToString() }
            : null;
}
