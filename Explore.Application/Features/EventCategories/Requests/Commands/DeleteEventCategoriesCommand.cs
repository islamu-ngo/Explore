using System;
using MediatR;

namespace Explore.Application.Features.EventCategories.Requests.Commands;

public class DeleteEventCategoriesCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
