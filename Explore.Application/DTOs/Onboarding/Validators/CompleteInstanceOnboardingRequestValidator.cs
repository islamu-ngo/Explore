// ABOUTME: FluentValidation validator for the minimal onboarding request payload.
// ABOUTME: Validates deployment mode and the Application-owned self-hosted site profile.

using FluentValidation;

namespace Explore.Application.DTOs.Onboarding.Validators;

public class CompleteInstanceOnboardingRequestValidator : AbstractValidator<CompleteInstanceOnboardingRequest>
{
    public CompleteInstanceOnboardingRequestValidator()
    {
        RuleFor(x => x.DeploymentMode)
            .IsInEnum()
            .WithMessage("DeploymentMode must be SingleTenant or MultiTenant.");

        RuleFor(x => x.SiteProfile)
            .NotNull()
            .WithMessage("SiteProfile is required.")
            .SetValidator(new SelfHostOnboardingProfileDtoValidator());

        RuleFor(x => x.InstanceName)
            .MaximumLength(200)
            .When(x => x.InstanceName is not null)
            .WithMessage("InstanceName must not exceed 200 characters.");
    }
}
