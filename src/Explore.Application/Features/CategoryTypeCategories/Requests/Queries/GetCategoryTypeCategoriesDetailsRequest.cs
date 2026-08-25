// ABOUTME: MediatR query request for fetching a single category-type/category link by ID.
// ABOUTME: Returns CategoryTypeCategoriesDto.
using Explore.Application.DTOs.CategoryTypeCategories;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Queries;

public sealed record GetCategoryTypeCategoriesDetailsRequest(Guid Id = default) : IRequest<CategoryTypeCategoriesDto>;
