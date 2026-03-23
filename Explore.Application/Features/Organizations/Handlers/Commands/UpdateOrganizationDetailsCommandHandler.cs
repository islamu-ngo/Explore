// ABOUTME: Handler for updating organization details — validates input and enforces OrgAdmin authorization.
// ABOUTME: Only OrgAdmin members may update their organization's profile fields.

using System.Linq;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization.Validators;
using Explore.Application.Features.Organizations.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Organizations.Handlers.Commands;

public class UpdateOrganizationDetailsCommandHandler : IRequestHandler<UpdateOrganizationDetailsCommand, BaseCommandResponse<Guid>>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;

    public UpdateOrganizationDetailsCommandHandler(
        IOrganizationRepository organizationRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IMapper mapper,
        HybridCache cache)
    {
        _organizationRepository = organizationRepository;
        _organizationMemberRepository = organizationMemberRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateOrganizationDetailsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateOrganizationDtoValidator();
        var validationResult = await validator.ValidateAsync(request.OrganizationDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Organization update failed due to validation errors.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        // Get the organization
        var organization = await _organizationRepository.GetById(request.Id);

        if (organization == null)
        {
            response.Success = false;
            response.Message = "Organization not found.";
            return response;
        }

        // Authorization check: Only admins can update the organization
        var members = await _organizationMemberRepository.GetMembersByOrganizationId(request.Id);
        if (Guid.TryParse(request.UserId, out Guid userGuid))
        {
            var requesterMember = members.FirstOrDefault(m => m.UserId == userGuid);
            if (requesterMember == null || requesterMember.RoleId != (int)RoleEnum.OrgAdmin)
            {
                response.Success = false;
                response.Message = "You are not authorized to update this organization.";
                return response;
            }
        }
        else
        {
            response.Success = false;
            response.Message = "Invalid user ID.";
            return response;
        }

        // Update the organization properties
        organization.FullName = request.OrganizationDto.FullName;
        organization.WebsiteUrl = request.OrganizationDto.WebsiteUrl;
        organization.Email = request.OrganizationDto.Email;
        organization.Country = request.OrganizationDto.Country;
        organization.City = request.OrganizationDto.City;
        organization.Postcode = request.OrganizationDto.Postcode.ToString();
        organization.Address = request.OrganizationDto.Address;

        await _organizationRepository.Update(organization);
        await _cache.RemoveAsync($"organization:detail:{organization.Id}", cancellationToken);

        response.Success = true;
        response.Message = "Organization updated successfully.";
        response.Id = organization.Id;
        return response;
    }
}
