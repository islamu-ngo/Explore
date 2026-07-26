// ABOUTME: Updates draft-editable event shell fields through a local workflow contract.
// ABOUTME: Keeps lifecycle status, session projections, and public API updates separate.

using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Update)]
public sealed class UpdateEventDraftCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid Id { get; set; }
    public required UpdateEventDraftRequestDto Draft { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
