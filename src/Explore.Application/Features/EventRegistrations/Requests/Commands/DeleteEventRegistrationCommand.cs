// ABOUTME: MediatR command for cancelling an event registration.
// ABOUTME: Carries the registration ID.
using System;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventRegistration, AuthorizationActions.Delete)]
public class DeleteEventRegistrationCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
