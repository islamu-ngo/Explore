using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Features.Tenants.Requests.Commands;
using Explore.Application.DTOs.Tenant.Validators;
using Explore.Application.Responses;
using Explore.Domain;
using FluentValidation;
using System;

namespace Explore.Application.Features.Tenants.Handlers.Commands
{
    public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, BaseCommandResponse<Guid>>
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateTenantDto> _validator;

        public CreateTenantCommandHandler(
            ITenantRepository tenantRepository,
            IMapper mapper,
            IValidator<CreateTenantDto> validator)
        {
            _tenantRepository = tenantRepository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validationResult = await _validator.ValidateAsync(request.TenantDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Tenant creation failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var tenant = _mapper.Map<Tenant>(request.TenantDto);
            tenant = await _tenantRepository.Create(tenant);

            response.Success = true;
            response.Id = tenant.Id;
            response.Message = "Tenant created successfully.";

            return response;
        }
    }
}
