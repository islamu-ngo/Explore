// ABOUTME: FluentValidation validator for the minimal onboarding request payload.
// ABOUTME: Validates that DeploymentMode is a defined enum value and InstanceName length is reasonable.

using FluentValidation;

namespace Explore.Application.DTOs.Onboarding.Validators;

public class CompleteInstanceOnboardingRequestValidator : AbstractValidator<CompleteInstanceOnboardingRequest>
{
    public CompleteInstanceOnboardingRequestValidator()
    {
        RuleFor(x => x.DeploymentMode)
            .IsInEnum()
            .WithMessage("DeploymentMode must be SingleTenant or MultiTenant.");

        RuleFor(x => x.InstanceName)
            .MaximumLength(200)
            .When(x => x.InstanceName is not null)
            .WithMessage("InstanceName must not exceed 200 characters.");
    }
}
