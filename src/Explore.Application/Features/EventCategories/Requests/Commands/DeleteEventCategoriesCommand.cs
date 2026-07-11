// ABOUTME: MediatR command for removing a category from an event.
// ABOUTME: Carries the junction record ID.
using System;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.EventCategories.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Update)]
public class DeleteEventCategoriesCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
