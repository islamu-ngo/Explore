using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
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
        private readonly IUserRepository _userRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly ITenantContext _tenantContext;
        private readonly IMapper _mapper;

        public CreateTenantUserCommandHandler(
            ITenantUserRepository tenantUserRepository,
            IUserRepository userRepository,
            ITenantRepository tenantRepository,
            IUserRoleRepository userRoleRepository,
            ITenantContext tenantContext,
            IMapper mapper)
        {
            _tenantUserRepository = tenantUserRepository;
            _userRepository = userRepository;
            _tenantRepository = tenantRepository;
            _userRoleRepository = userRoleRepository;
            _tenantContext = tenantContext;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateTenantUserCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validator = new CreateTenantUserDtoValidator(_userRepository, _tenantRepository, _userRoleRepository);
            var validationResult = await validator.ValidateAsync(request.TenantUserDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Tenant User creation failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var tenantUser = _mapper.Map<TenantUser>(request.TenantUserDto);

            // Set TenantId from the request context
            tenantUser.TenantId = _tenantContext.TenantId;

            tenantUser = await _tenantUserRepository.Create(tenantUser);

            response.Success = true;
            response.Id = tenantUser.Id;
            response.Message = "Tenant User created successfully.";

            return response;
        }
    }
}
