// ABOUTME: MediatR command for deleting a category by ID.
// ABOUTME: Carries the target category ID.
using System;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.Categories.Requests.Commands;

[AuthorizeResource(ResourceKinds.Category, AuthorizationActions.Delete)]
public class DeleteCategoryCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
