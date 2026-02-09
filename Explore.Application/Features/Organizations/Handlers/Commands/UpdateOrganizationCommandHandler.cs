using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.Organizations.Requests.Commands;
using Explore.Domain;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Organizations.Handlers.Commands;

public class UpdateOrganizationCommandHandler : IRequestHandler<UpdateOrganizationCommand, Unit>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IApprovalStatusRepository _statusTypeRepository;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;

    public UpdateOrganizationCommandHandler(IOrganizationRepository organizationRepository, IApprovalStatusRepository statusTypeRepository, IMapper mapper, HybridCache cache)
    {
        _organizationRepository = organizationRepository;
        _statusTypeRepository = statusTypeRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<Unit> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetById(request.Id);
        if (organization == null)
        {
            throw new NotFoundException(nameof(Organization), request.Id);
        }

        if (request.OrganizationApprovalStatusDto != null)
        {
            var validator = new UpdateOrganizationApprovalStatusDtoValidator(_statusTypeRepository);
            var validationResult = await validator.ValidateAsync(request.OrganizationApprovalStatusDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
            organization.ApprovalStatusId = request.OrganizationApprovalStatusDto.ApprovalStatusId;
            await _organizationRepository.Update(organization);
            await _cache.RemoveAsync($"organization:detail:{organization.Id}", cancellationToken);
        }

        return Unit.Value;
    }
}
