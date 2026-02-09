using System;
using Explore.Application.DTOs.EventCategories;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCategories.Requests.Commands;

public class CreateEventCategoriesCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateEventCategoriesDto EventCategoriesDto { get; set; }
}
