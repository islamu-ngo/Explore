// ABOUTME: MediatR command for creating a new tag.
// ABOUTME: Carries the CreateTagDto payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Tag;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Tags.Requests.Commands;

[AuthorizeResource("tag", PermissionAction.Create)]
public class CreateTagCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateTagDto TagDto { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        TagDto.TenantId != Guid.Empty
            ? new Dictionary<string, object> { ["tenantId"] = TagDto.TenantId.ToString() }
            : null;
}
