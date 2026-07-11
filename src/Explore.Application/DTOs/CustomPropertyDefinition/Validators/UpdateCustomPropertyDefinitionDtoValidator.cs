// ABOUTME: Validates shared Layer 3 custom-property definition update payloads.
// ABOUTME: Reuses create rules and additionally requires an existing identifier.

using FluentValidation;

namespace Explore.Application.DTOs.CustomPropertyDefinition.Validators;

public class UpdateCustomPropertyDefinitionDtoValidator : AbstractValidator<UpdateCustomPropertyDefinitionDto>
{
    public UpdateCustomPropertyDefinitionDtoValidator()
    {
        Include(new CreateCustomPropertyDefinitionDtoValidator());

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.ExpectedConcurrencyStamp)
            .NotEmpty().WithMessage("ExpectedConcurrencyStamp is required.");
    }
}
