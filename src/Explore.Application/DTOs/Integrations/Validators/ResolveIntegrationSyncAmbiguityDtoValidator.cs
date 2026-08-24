// ABOUTME: Validates bounded evidence-based IntegrationSync ambiguity recovery requests.
// ABOUTME: Rejects undefined decisions and blank or oversized opaque evidence references.

using FluentValidation;

namespace Explore.Application.DTOs.Integrations.Validators;

public sealed class ResolveIntegrationSyncAmbiguityDtoValidator : AbstractValidator<ResolveIntegrationSyncAmbiguityDto>
{
    public ResolveIntegrationSyncAmbiguityDtoValidator()
    {
        RuleFor(request => request.Decision).IsInEnum();
        RuleFor(request => request.EvidenceReference).NotEmpty().MaximumLength(200);
    }
}
