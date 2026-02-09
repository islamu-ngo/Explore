using System;
using Explore.Application.DTOs.EventCategories;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCategories.Requests.Commands;

public class UpdateEventCategoriesCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateEventCategoriesDto EventCategoriesDto { get; set; }
}
