using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantUser;
using Explore.Application.DTOs.TenantUser.Validators;
using Explore.Application.Features.TenantUsers.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.TenantUsers.Handlers.Commands;

public class UpdateTenantUserCommandHandler : IRequestHandler<UpdateTenantUserCommand, BaseCommandResponse<Guid>>
{
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IMapper _mapper;

    public UpdateTenantUserCommandHandler(
        ITenantUserRepository tenantUserRepository,
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IUserRoleRepository userRoleRepository,
        IMapper mapper)
    {
        _tenantUserRepository = tenantUserRepository;
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _userRoleRepository = userRoleRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateTenantUserCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateTenantUserDtoValidator(_userRepository, _tenantRepository, _userRoleRepository);
        var validationResult = await validator.ValidateAsync(request.TenantUserDto, cancellationToken);

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
