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
    public class UpdateTenantUserCommandHandler : IRequestHandler<UpdateTenantUserCommand, BaseCommandResponse<Guid>>
    {
        private readonly ITenantUserRepository _tenantUserRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<UpdateTenantUserDto> _validator;

        public UpdateTenantUserCommandHandler(
            ITenantUserRepository tenantUserRepository,
            IMapper mapper,
            IValidator<UpdateTenantUserDto> validator)
        {
            _tenantUserRepository = tenantUserRepository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(UpdateTenantUserCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            var validationResult = await _validator.ValidateAsync(request.TenantUserDto);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Tenant User update failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            var existingTenantUser = await _tenantUserRepository.GetById(request.TenantUserDto.Id);
            if (existingTenantUser == null)
            {
                response.Success = false;
                response.Message = "Tenant User not found.";
                return response;
            }

            _mapper.Map(request.TenantUserDto, existingTenantUser);
            await _tenantUserRepository.Update(existingTenantUser);

            response.Success = true;
            response.Id = existingTenantUser.Id;
            response.Message = "Tenant User updated successfully.";

            return response;
        }
    }
}
