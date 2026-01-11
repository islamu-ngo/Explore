using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantUser;
using Explore.Application.Features.TenantUsers.Requests.Commands;
using Explore.Application.DTOs.TenantUser.Validators;
using Explore.Application.Responses;
using Explore.Domain;
using FluentValidation;

namespace Explore.Application.Features.TenantUsers.Handlers.Commands
{
    public class CreateTenantUserCommandHandler : IRequestHandler<CreateTenantUserCommand, BaseCommandResponse<Guid>>
    {
        private readonly ITenantUserRepository _tenantUserRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateTenantUserDto> _validator;

        public CreateTenantUserCommandHandler(
            ITenantUserRepository tenantUserRepository,
            IMapper mapper,
            IValidator<CreateTenantUserDto> validator)
        {
            _tenantUserRepository = tenantUserRepository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateTenantUserCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validationResult = await _validator.ValidateAsync(request.TenantUserDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Tenant User creation failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var tenantUser = _mapper.Map<TenantUser>(request.TenantUserDto);
            tenantUser = await _tenantUserRepository.Create(tenantUser);

            response.Success = true;
            response.Id = tenantUser.Id;
            response.Message = "Tenant User created successfully.";

            return response;
        }
    }
}
