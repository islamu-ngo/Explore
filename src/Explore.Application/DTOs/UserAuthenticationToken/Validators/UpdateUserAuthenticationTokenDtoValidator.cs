// ABOUTME: Validator for user authentication-token update requests.
// ABOUTME: Validates token metadata while ownership is enforced by user-scoped lookup.

using Explore.Application.DTOs.UserAuthenticationToken;
using FluentValidation;

namespace Explore.Application.DTOs.UserAuthenticationToken.Validators;

public class UpdateUserAuthenticationTokenDtoValidator : AbstractValidator<UpdateUserAuthenticationTokenDto>
{
    public UpdateUserAuthenticationTokenDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User Authentication Token ID is required");

        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider is required")
            .MaximumLength(500).WithMessage("Provider cannot exceed 500 characters");

        RuleFor(x => x.AccessToken)
            .NotEmpty().WithMessage("Access token is required")
            .MaximumLength(500).WithMessage("Access token cannot exceed 500 characters");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required")
            .MaximumLength(500).WithMessage("Refresh token cannot exceed 500 characters");

        RuleFor(x => x.PdsHost)
            .MaximumLength(500).WithMessage("PDS host cannot exceed 500 characters");

        RuleFor(x => x.DpopKey)
            .NotEmpty().WithMessage("DPoP key is required")
            .MaximumLength(500).WithMessage("DPoP key cannot exceed 500 characters");

        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("ID token is required")
            .MaximumLength(500).WithMessage("ID token cannot exceed 500 characters");
    }
}
