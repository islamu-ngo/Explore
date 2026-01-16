using FluentValidation;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantUser;
using System;

namespace Explore.Application.DTOs.TenantUser.Validators
{
    public class CreateTenantUserDtoValidator : AbstractValidator<CreateTenantUserDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IUserRoleRepository _userRoleRepository;

        public CreateTenantUserDtoValidator(IUserRepository userRepository, ITenantRepository tenantRepository, IUserRoleRepository userRoleRepository)
        {
            _userRepository = userRepository;
            _tenantRepository = tenantRepository;
            _userRoleRepository = userRoleRepository;

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required")
                .MustAsync(UserExists)
                .WithMessage("User does not exist");

            // TenantId is set by the handler from context, not by the client
            // No validation needed here

            RuleFor(x => x.UserRoleId)
                .NotEmpty().WithMessage("User Role ID is required")
                .MustAsync(UserRoleExists)
                .WithMessage("User Role does not exist");
        }

        private async Task<bool> UserExists(Guid userId, CancellationToken cancellationToken)
        {
            return await _userRepository.Exists(userId);
        }

        private async Task<bool> UserRoleExists(int userRoleId, CancellationToken cancellationToken)
        {
            var role = await _userRoleRepository.GetById(userRoleId);
            return role != null;
        }
    }
}
