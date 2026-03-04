// ABOUTME: Validates CreateTenantMemberDto ensuring referenced user and role exist.
// ABOUTME: Manually instantiated per project convention (no DI for validators).

using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.TenantMember.Validators;

public class CreateTenantMemberDtoValidator : AbstractValidator<CreateTenantMemberDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public CreateTenantMemberDtoValidator(IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required")
            .MustAsync(UserExists)
            .WithMessage("User does not exist");

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
