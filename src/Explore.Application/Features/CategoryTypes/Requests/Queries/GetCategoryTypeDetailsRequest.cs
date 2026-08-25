// ABOUTME: MediatR query request for fetching a single category type by ID.
// ABOUTME: Returns CategoryTypeDto.
using Explore.Application.DTOs.CategoryType;
using MediatR;

namespace Explore.Application.Features.CategoryTypes.Requests.Queries;

public sealed record GetCategoryTypeDetailsRequest(int Id = default) : IRequest<CategoryTypeDto>;
