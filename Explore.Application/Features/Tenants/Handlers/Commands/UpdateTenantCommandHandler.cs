using System;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Tenant;
using Explore.Application.DTOs.Tenant.Validators;
using Explore.Application.Features.Tenants.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.Tenants.Handlers.Commands;

public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, BaseCommandResponse<Guid>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<UpdateTenantDto> _validator;

    public UpdateTenantCommandHandler(
        ITenantRepository tenantRepository,
        IMapper mapper,
        IValidator<UpdateTenantDto> validator)
    {
        _tenantRepository = tenantRepository;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validationResult = await _validator.ValidateAsync(request.TenantDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Tenant update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var existingTenant = await _tenantRepository.GetById(request.TenantDto.Id);
        if (existingTenant == null)
        {
            response.Success = false;
            response.Message = "Tenant not found.";
            return response;
        }

        _mapper.Map(request.TenantDto, existingTenant);
        await _tenantRepository.Update(existingTenant);

        response.Success = true;
        response.Id = existingTenant.Id;
        response.Message = "Tenant updated successfully.";

        return response;
    }
}
