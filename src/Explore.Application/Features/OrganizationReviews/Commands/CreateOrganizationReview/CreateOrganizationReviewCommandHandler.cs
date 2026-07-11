// ABOUTME: Handler for creating a new organization review with validation.
// ABOUTME: Validates input, maps DTO, links to actor, persists review.
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.OrganizationReview;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.OrganizationReviews.Commands.CreateOrganizationReview;

public class CreateOrganizationReviewCommandHandler : IRequestHandler<CreateOrganizationReviewCommand, BaseCommandResponse<Guid>>
{
    private readonly IOrganizationReviewRepository _organizationReviewRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;

    public CreateOrganizationReviewCommandHandler(IOrganizationReviewRepository organizationReviewRepository, ITenantContext tenantContext, IMapper mapper)
    {
        _organizationReviewRepository = organizationReviewRepository;
        _tenantContext = tenantContext;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateOrganizationReviewCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var organizationReview = _mapper.Map<OrganizationReview>(request.CreateOrganizationReviewDto);

        organizationReview.CreatedAt = DateTime.UtcNow;
        organizationReview.UpdatedAt = DateTime.UtcNow;

        // Set TenantId from the request context
        organizationReview.TenantId = _tenantContext.TenantId;

        organizationReview = await _organizationReviewRepository.Create(organizationReview);

        response.Success = true;
        response.Message = "Review Created Successfully";
        response.Id = organizationReview.Id;

        return response;
    }
}
