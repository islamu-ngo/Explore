// ABOUTME: MediatR command for registering a user for an event.
// ABOUTME: Carries the CreateEventRegistrationDto payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Requests.Commands;

[AuthorizeResource("event_registration", AuthorizationActions.Create)]
public class CreateEventRegistrationCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateEventRegistrationDto EventRegistrationDto { get; set; }

    string? ISecureRequest.ResourceId => EventRegistrationDto.EventSessionId.ToString();
}
