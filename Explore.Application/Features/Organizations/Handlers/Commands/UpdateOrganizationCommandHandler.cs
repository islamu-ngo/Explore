using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.Organizations.Requests.Commands;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Features.Organizations.Handlers.Commands
{
    public class UpdateOrganizationCommandHandler : IRequestHandler<UpdateOrganizationCommand, Unit>
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IMapper _mapper;

        public UpdateOrganizationCommandHandler(IOrganizationRepository organizationRepository, IMapper mapper)
        {
            _organizationRepository = organizationRepository;
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
                organization.StatusTypeId = request.OrganizationStatusTypeDto.StatusTypeId;
                await _organizationRepository.Update(organization);
            }

            return Unit.Value;
        }
    }
}
