// ABOUTME: Handler for the admin-only organization approval status action.
// ABOUTME: Validates lookup status, updates the organization lifecycle field, and invalidates organization detail cache.
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.Organizations.Requests.Commands;
using Explore.Domain;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Organizations.Handlers.Commands;

public class UpdateOrganizationApprovalStatusCommandHandler : IRequestHandler<UpdateOrganizationApprovalStatusCommand, Unit>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IApprovalStatusRepository _statusTypeRepository;
    private readonly HybridCache _cache;

    public UpdateOrganizationApprovalStatusCommandHandler(
        IOrganizationRepository organizationRepository,
        IApprovalStatusRepository statusTypeRepository,
        HybridCache cache)
    {
        _organizationRepository = organizationRepository;
        _statusTypeRepository = statusTypeRepository;
        _cache = cache;
    }

    public async Task<Unit> Handle(UpdateOrganizationApprovalStatusCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetById(request.OrganizationId);
        if (organization == null)
        {
            throw new NotFoundException(nameof(Organization), request.OrganizationId);
        }

        var validator = new UpdateOrganizationApprovalStatusDtoValidator(_statusTypeRepository);
        var validationResult = await validator.ValidateAsync(request.ApprovalStatusDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult);
        }

        organization.ApprovalStatusId = request.ApprovalStatusDto.ApprovalStatusId;

        await _organizationRepository.Update(organization);
        await _cache.RemoveAsync($"organization:detail:{organization.Id}", cancellationToken);

        return Unit.Value;
    }
}
