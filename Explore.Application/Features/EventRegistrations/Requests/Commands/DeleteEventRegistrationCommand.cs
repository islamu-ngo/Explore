using System;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Requests.Commands;

[AuthorizeResource("event_registration", PermissionAction.Delete)]
public class DeleteEventRegistrationCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
