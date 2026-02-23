using System;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.Categories.Requests.Commands;

[AuthorizeResource("category", PermissionAction.Delete)]
public class DeleteCategoryCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
