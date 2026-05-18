// ABOUTME: MediatR command for updating an event registration.
// ABOUTME: Carries the UpdateEventRegistrationDto payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventRegistration, AuthorizationActions.Update)]
public class UpdateEventRegistrationCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateEventRegistrationDto EventRegistrationDto { get; set; }

    string? ISecureRequest.ResourceId => EventRegistrationDto.Id.ToString();
}
