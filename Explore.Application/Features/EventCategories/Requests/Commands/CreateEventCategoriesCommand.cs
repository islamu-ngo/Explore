using Explore.Application.DTOs.EventCategories;
using Explore.Application.Responses;
using MediatR;
using System;

namespace Explore.Application.Features.EventCategories.Requests.Commands
{
    public class CreateEventCategoriesCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateEventCategoriesDto EventCategoriesDto { get; set; }
    }
}
