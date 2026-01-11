using FluentValidation;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserAuthenticationToken;
using System;

namespace Explore.Application.DTOs.UserAuthenticationToken.Validators
{
    public class UpdateUserAuthenticationTokenDtoValidator : AbstractValidator<UpdateUserAuthenticationTokenDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly ITenantRepository _tenantRepository;

        public UpdateUserAuthenticationTokenDtoValidator(IUserRepository userRepository, ITenantRepository tenantRepository)
        {
            _userRepository = userRepository;
            _tenantRepository = tenantRepository;

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("User Authentication Token ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required")
                .MustAsync(UserExists)
                .WithMessage("User does not exist");

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("Tenant ID is required")
                .MustAsync(TenantExists)
                .WithMessage("Tenant does not exist");

            RuleFor(x => x.Provider)
                .NotEmpty().WithMessage("Provider is required")
                .MaximumLength(500).WithMessage("Provider cannot exceed 500 characters");
        }

        private async Task<bool> UserExists(Guid userId, CancellationToken cancellationToken)
        {
            return await _userRepository.Exists(userId);
        }

        private async Task<bool> TenantExists(Guid tenantId, CancellationToken cancellationToken)
        {
            return await _tenantRepository.Exists(tenantId);
        }
    }
}
