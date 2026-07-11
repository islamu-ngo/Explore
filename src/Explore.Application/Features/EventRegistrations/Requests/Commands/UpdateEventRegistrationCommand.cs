// ABOUTME: MediatR command for route-ID EventRegistration PATCH updates.
// ABOUTME: Carries the expected concurrency stamp and grouped update payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventRegistration, AuthorizationActions.Update)]
public class UpdateEventRegistrationCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventRegistrationId { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
    public required UpdateEventRegistrationDto EventRegistrationDto { get; set; }

    string? ISecureRequest.ResourceId => EventRegistrationId.ToString();
}
