using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Category;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Categories.Requests.Commands;

[AuthorizeResource("category", PermissionAction.Update)]
public class UpdateCategoryCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateCategoryDto CategoryDto { get; set; }

    string? ISecureRequest.ResourceId => CategoryDto.Id.ToString();
}
