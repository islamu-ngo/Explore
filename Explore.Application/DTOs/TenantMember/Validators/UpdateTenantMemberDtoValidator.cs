// ABOUTME: Validates UpdateTenantMemberDto ensuring ID is present and referenced user/role exist.
// ABOUTME: Manually instantiated per project convention (no DI for validators).

using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.TenantMember.Validators;

public class UpdateTenantMemberDtoValidator : AbstractValidator<UpdateTenantMemberDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public UpdateTenantMemberDtoValidator(IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Tenant Member ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required")
            .MustAsync(UserExists)
            .WithMessage("User does not exist");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required")
            .MustAsync(async (roleId, cancellation) =>
            {
                var role = await _roleRepository.GetByIdAsync(roleId);
                return role != null;
            })
            .WithMessage("Role does not exist");
    }

    private async Task<bool> UserExists(Guid userId, CancellationToken cancellationToken)
    {
        return await _userRepository.Exists(userId);
    }
}
