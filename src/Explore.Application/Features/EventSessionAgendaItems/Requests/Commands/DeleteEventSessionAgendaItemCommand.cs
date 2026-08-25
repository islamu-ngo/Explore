// ABOUTME: MediatR command for deleting an agenda item by ID.
// ABOUTME: Carries the target agenda item ID.
using System;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventSessionAgendaItem, AuthorizationActions.Delete)]
public sealed record DeleteEventSessionAgendaItemCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; init; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
