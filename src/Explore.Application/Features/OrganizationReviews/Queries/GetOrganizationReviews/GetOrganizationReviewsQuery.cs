// ABOUTME: MediatR query for fetching all reviews for a specific organization.
// ABOUTME: Returns IEnumerable<OrganizationReviewDto>.
using Explore.Application.DTOs.OrganizationReview;
using MediatR;

namespace Explore.Application.Features.OrganizationReviews.Queries.GetOrganizationReviews;

public sealed record GetOrganizationReviewsQuery(Guid OrganizationId = default) : IRequest<List<OrganizationReviewDto>>;
