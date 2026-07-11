// ABOUTME: Validator for user authentication-token create requests.
// ABOUTME: Validates token metadata while ownership is derived by the handler.

using Explore.Application.DTOs.UserAuthenticationToken;
using FluentValidation;

namespace Explore.Application.DTOs.UserAuthenticationToken.Validators;

public class CreateUserAuthenticationTokenDtoValidator : AbstractValidator<CreateUserAuthenticationTokenDto>
{
    public CreateUserAuthenticationTokenDtoValidator()
    {
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
