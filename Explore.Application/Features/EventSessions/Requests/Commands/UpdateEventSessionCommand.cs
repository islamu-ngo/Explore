// ABOUTME: MediatR command for PATCH-based EventSession property updates.
// ABOUTME: Carries route ID, If-Match concurrency stamp, and grouped update payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventSession, AuthorizationActions.Update)]
public class UpdateEventSessionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventSessionId { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
    public required UpdateEventSessionDto EventSessionDto { get; set; }

    string? ISecureRequest.ResourceId => EventSessionId.ToString();
}
