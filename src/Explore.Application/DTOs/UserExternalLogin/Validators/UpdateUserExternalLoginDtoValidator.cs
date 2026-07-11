using System;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserExternalLogin;
using FluentValidation;

namespace Explore.Application.DTOs.UserExternalLogin.Validators;

public class UpdateUserExternalLoginDtoValidator : AbstractValidator<UpdateUserExternalLoginDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;

    public UpdateUserExternalLoginDtoValidator(IUserRepository userRepository, ITenantRepository tenantRepository)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User External Login ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required")
            .MustAsync(UserExists)
            .WithMessage("User does not exist");

        // TenantId is set by the handler from context, not by the client
        // No validation needed here

        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider is required")
            .MaximumLength(255).WithMessage("Provider cannot exceed 255 characters");
    }

    private async Task<bool> UserExists(Guid userId, CancellationToken cancellationToken)
    {
        return await _userRepository.Exists(userId);
    }
}
