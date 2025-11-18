using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Organizations.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Organizations.Handlers.Commands
{
    public class UpdateOrganizationDetailsCommandHandler : IRequestHandler<UpdateOrganizationDetailsCommand, BaseCommandResponse<Guid>>
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IMapper _mapper;

        public UpdateOrganizationDetailsCommandHandler(IOrganizationRepository organizationRepository, IMapper mapper)
        {
            _organizationRepository = organizationRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(UpdateOrganizationDetailsCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            // Get the organization
            var organization = await _organizationRepository.GetById(request.Id);

            if (organization == null)
            {
                response.Success = false;
                response.Message = "Organization not found.";
                return response;
            }

            // Authorization check: Only the creator can update the organization
            if (organization.CreatedByUserId != request.UserId)
            {
                response.Success = false;
                response.Message = "You are not authorized to update this organization.";
                return response;
            }

            // Update the organization properties
            organization.FullName = request.OrganizationDto.FullName;
            organization.WebsiteUrl = request.OrganizationDto.WebsiteUrl;
            organization.Email = request.OrganizationDto.Email;
            organization.Country = request.OrganizationDto.Country;
            organization.City = request.OrganizationDto.City;
            organization.Postcode = request.OrganizationDto.Postcode;
            organization.Address = request.OrganizationDto.Address;

            await _organizationRepository.Update(organization);

            response.Success = true;
            response.Message = "Organization updated successfully.";
            response.Id = organization.Id;
            return response;
        }
    }
}
