using System;
using Explore.Application.DTOs.Category;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Categories.Requests.Commands
{
    public class UpdateCategoryCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public UpdateCategoryDto CategoryDto { get; set; }
    }
}
