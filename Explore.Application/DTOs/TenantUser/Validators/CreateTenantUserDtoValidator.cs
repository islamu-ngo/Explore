using System;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantUser;
using FluentValidation;

namespace Explore.Application.DTOs.TenantUser.Validators;

public class CreateTenantUserDtoValidator : AbstractValidator<CreateTenantUserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IRoleRepository _roleRepository;

    public CreateTenantUserDtoValidator(IUserRepository userRepository, ITenantRepository tenantRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _roleRepository = roleRepository;

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required")
            .MustAsync(UserExists)
            .WithMessage("User does not exist");

        // TenantId is set by the handler from context, not by the client
        // No validation needed here

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required")
            .MustAsync(RoleExists)
            .WithMessage("Role does not exist");
    }

    private async Task<bool> UserExists(Guid userId, CancellationToken cancellationToken)
    {
        return await _userRepository.Exists(userId);
    }

    private async Task<bool> RoleExists(int roleId, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        return role != null;
    }
}
