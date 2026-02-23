using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventCategories;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCategories.Requests.Commands;

[AuthorizeResource("event", PermissionAction.Update)]
public class CreateEventCategoriesCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateEventCategoriesDto EventCategoriesDto { get; set; }

    string? ISecureRequest.ResourceId => EventCategoriesDto.EventId.ToString();
}
