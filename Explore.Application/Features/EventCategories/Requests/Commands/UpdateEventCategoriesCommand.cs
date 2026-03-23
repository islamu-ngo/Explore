// ABOUTME: MediatR command for updating an event-category link.
// ABOUTME: Carries the UpdateEventCategoriesDto payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventCategories;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCategories.Requests.Commands;

[AuthorizeResource("event", PermissionAction.Update)]
public class UpdateEventCategoriesCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateEventCategoriesDto EventCategoriesDto { get; set; }

    string? ISecureRequest.ResourceId => EventCategoriesDto.EventId.ToString();
}
