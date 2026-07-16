// ABOUTME: MediatR command for cancelling an event registration.
// ABOUTME: Carries the registration ID plus a JSON-hidden persisted-owner snapshot bound by authorization.
using System;
using System.Text.Json.Serialization;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventRegistration, AuthorizationActions.Delete)]
public class DeleteEventRegistrationCommand : IRequest<bool>, ISecureRequest, IPersistedUserOwnerBoundRequest
{
    public Guid Id { get; set; }

    [JsonIgnore]
    public Guid? ExpectedOwnerUserId { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
