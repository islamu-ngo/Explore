// ABOUTME: Validates grouped shared custom-property definition PATCH payload shape.
// ABOUTME: Rejects empty groups before a handler validates the merged persisted candidate.

using FluentValidation;
using Explore.Application.DTOs.CustomPropertyDefinition;

namespace Explore.Application.DTOs.CustomPropertyDefinition.Validators;

public sealed class UpdateCustomPropertyDefinitionDtoValidator : AbstractValidator<UpdateCustomPropertyDefinitionDto>
{
    public UpdateCustomPropertyDefinitionDtoValidator()
    {
        RuleFor(x => x).Must(x => x.Metadata is not null || x.Validation is not null || x.Options is not null)
            .WithMessage("At least one update group is required.");
        RuleFor(x => x.Metadata).Must(x => x is not null && (x.Namespace is not null || x.Key is not null || x.DisplayName is not null || x.Description.HasValue || x.PropertyType.HasValue || x.IsRequired.HasValue || x.IsMulti.HasValue || x.IsActive.HasValue || x.SortOrder.HasValue || x.ExposureLevel.HasValue || x.IsSearchable.HasValue || x.IsFilterable.HasValue || x.IsExportable.HasValue || x.IsModerationRelevant.HasValue || x.IsAnalyticsRelevant.HasValue || x.IsSystemOwned.HasValue)).When(x => x.Metadata is not null).WithMessage("Metadata must contain at least one update.");
        RuleFor(x => x.Validation).Must(x => x is not null && (x.DefaultTextValue.HasValue || x.DefaultNumberValue.HasValue || x.DefaultBooleanValue.HasValue || x.DefaultDateTimeValue.HasValue || x.MinLength.HasValue || x.MaxLength.HasValue || x.RegexPattern.HasValue || x.MinNumber.HasValue || x.MaxNumber.HasValue || x.MinDateTime.HasValue || x.MaxDateTime.HasValue || x.AllowedUrlSchemes.HasValue)).When(x => x.Validation is not null).WithMessage("Validation must contain at least one update.");
        RuleFor(x => x.Options!.Items).NotNull().When(x => x.Options is not null).WithMessage("Options.items is required when options is supplied.");
    }
}
