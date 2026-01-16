using System;
using Explore.Application.DTOs.Category;
using MediatR;

namespace Explore.Application.Features.Categories.Requests.Queries
{
    public class GetCategoryDetailsRequest : IRequest<CategoryDto>
    {
        public Guid Id { get; set; }
    }
}
