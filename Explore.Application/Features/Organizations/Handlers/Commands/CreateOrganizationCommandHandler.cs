using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Organizations.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.Organizations.Handlers.Commands
{
    public class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, BaseCommandResponse<Guid>>
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IMapper _mapper;

        public CreateOrganizationCommandHandler(IOrganizationRepository organizationRepository, IMapper mapper)
        {
            _organizationRepository = organizationRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();
            //var validator = new CreateOrganizationDtoValidator();
            //var validationResult = await validator.ValidateAsync(request.OrganizationDto);

            //if (!validationResult.IsValid)
            //{
            //    response.Success = false;
            //    response.Message = "Organization creation failed.";
            //    response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            //    return response;
            //}

            var organization = _mapper.Map<Organization>(request.OrganizationDto);

            organization.ApprovalStatusId = (int)StatusTypeEnum.Pending;

            // why is the below code adding to the members list inside organization domain class? that list should be readonly, add directly inside organizationmember. also no owner role, just admin.
            // Add creator as Owner
            //if (Guid.TryParse(request.UserId, out Guid userGuid))
            //{
            //    organization.Members.Add(new OrganizationMember
            //    {
            //        UserId = userGuid,
            //        Role = OrganizationRole.Creator,
            //        Email = request.OrganizationDto.Email // Fallback to org email as we don't have user email here
            //    });
            //}

            organization = await _organizationRepository.Create(organization);

            response.Success = true;
            response.Message = "Organization created successfully.";
            response.Id = organization.Id;
            return response;
        }
    }
}
