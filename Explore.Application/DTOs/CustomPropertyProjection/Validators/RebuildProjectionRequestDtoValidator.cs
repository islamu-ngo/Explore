// ABOUTME: Validates rebuild request payload ensuring tenant ID is present and batch size is within bounds.
// ABOUTME: Manually instantiated per project convention (no DI).

using FluentValidation;

namespace Explore.Application.DTOs.CustomPropertyProjection.Validators;

public class RebuildProjectionRequestDtoValidator : AbstractValidator<RebuildProjectionRequestDto>
{
    public RebuildProjectionRequestDtoValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.BatchSize)
            .GreaterThan(0).When(x => x.BatchSize.HasValue)
            .WithMessage("BatchSize must be greater than 0.");
    }
}
