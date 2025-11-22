using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.Organizations.Requests.Commands;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.Organization.Validators;
using Explore.Domain;

namespace Explore.Application.Features.Organizations.Handlers.Commands
{
    public class UpdateOrganizationCommandHandler : IRequestHandler<UpdateOrganizationCommand, Unit>
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IStatusTypeRepository _statusTypeRepository;
        private readonly IMapper _mapper;

        public UpdateOrganizationCommandHandler(IOrganizationRepository organizationRepository, IStatusTypeRepository statusTypeRepository, IMapper mapper)
        {
            _organizationRepository = organizationRepository;
            _statusTypeRepository = statusTypeRepository;
            _mapper = mapper;
        }

        public async Task<Unit> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
        {
            var organization = await _organizationRepository.GetById(request.Id);
            if (organization == null)
            {
                throw new NotFoundException(nameof(Organization), request.Id);
            }

            if (request.OrganizationStatusTypeDto != null)
            {
                var validator = new UpdateOrganizationStatusTypeDtoValidator(_statusTypeRepository);
                var validationResult = await validator.ValidateAsync(request.OrganizationStatusTypeDto);
                if (!validationResult.IsValid)
                {
                    throw new ValidationException(validationResult);
                }
                organization.StatusTypeId = request.OrganizationStatusTypeDto.StatusTypeId;
                await _organizationRepository.Update(organization);
            }

            return Unit.Value;
        }
    }
}
