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
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public CreateOrganizationCommandHandler(
            IOrganizationRepository organizationRepository, 
            IUserRepository userRepository,
            IMapper mapper)
        {
            _organizationRepository = organizationRepository;
            _userRepository = userRepository;
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

            organization.StatusTypeId = (int)StatusTypeEnum.Pending;
            organization.CreatedByUserId = request.UserId;
            organization.CreatedAt = DateTime.UtcNow;

            // Add creator as Owner with their actual email
            if (Guid.TryParse(request.UserId, out Guid userGuid))
            {
                var user = await _userRepository.GetByIdAsync(userGuid);
                var creatorEmail = user?.Email ?? request.OrganizationDto.Email; // Fallback to org email if user not found
                
                organization.Members.Add(new OrganizationMember
                {
                    UserId = userGuid,
                    Role = OrganizationRole.Creator,
                    Email = creatorEmail
                });
            }

            organization = await _organizationRepository.Create(organization);

            response.Success = true;
            response.Message = "Organization created successfully.";
            response.Id = organization.Id;
            return response;
        }
    }
}
