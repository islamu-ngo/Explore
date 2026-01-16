using Explore.Application.DTOs.EventCategories;
using Explore.Application.Responses;
using MediatR;
using System;

namespace Explore.Application.Features.EventCategories.Requests.Commands
{
    public class UpdateEventCategoriesCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public UpdateEventCategoriesDto EventCategoriesDto { get; set; }
    }
}
