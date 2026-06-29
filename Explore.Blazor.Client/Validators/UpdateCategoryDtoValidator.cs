// ABOUTME: Client-side validator for grouped category update dialogs.
// ABOUTME: Mirrors the PATCH wrapper shape generated from the API contract.

using Explore.Blazor.Client.Clients;
using FluentValidation;

namespace Explore.Blazor.Client.Validators;

public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(x => x.FullName!.Value)
            .NotEmpty()
            .When(x => x.FullName is not null);

        RuleFor(x => x.MasterCode!.Value)
            .NotEmpty()
            .When(x => x.MasterCode is not null);

        RuleFor(x => x)
            .Must(x => x.FullName is not null || x.MasterCode is not null || x.Parent is not null)
            .WithMessage("At least one category field must be updated.");
    }
}
