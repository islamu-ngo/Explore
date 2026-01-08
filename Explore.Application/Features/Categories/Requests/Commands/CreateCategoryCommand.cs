using System;
using Explore.Application.DTOs.Category;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Categories.Requests.Commands
{
    public class CreateCategoryCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateCategoryDto CategoryDto { get; set; }
    }
}
