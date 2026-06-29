// ABOUTME: MediatR command for route-ID event-tag link updates.
// ABOUTME: Carries expected concurrency and grouped relationship update payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventTags;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTags.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Update)]
public class UpdateEventTagsCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventTagId { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
    public required UpdateEventTagsDto EventTagsDto { get; set; }
    public Guid EventId { get; set; }
    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => EventId.ToString();
}
