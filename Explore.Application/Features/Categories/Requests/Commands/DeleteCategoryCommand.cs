using System;
using MediatR;

namespace Explore.Application.Features.Categories.Requests.Commands;

public class DeleteCategoryCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
