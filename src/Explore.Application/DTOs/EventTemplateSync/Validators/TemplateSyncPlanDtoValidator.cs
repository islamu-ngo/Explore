// ABOUTME: Manual validator for event-template sync apply plans enforcing positive versions and platform-default payload limits.
// ABOUTME: The sync service applies tenant-specific quota enforcement separately before opening its transaction.

using Explore.Application.DTOs.EventTemplateSync;
using Explore.Domain.Settings.Definitions;
using FluentValidation;

namespace Explore.Application.DTOs.EventTemplateSync.Validators;

public sealed class TemplateSyncPlanDtoValidator : AbstractValidator<TemplateSyncPlanDto>
{
    private static readonly int MaxChangeCount = int.Parse(CustomPropertyQuotaSettingDefinitions.SyncApplyMaxChangeCount.DefaultValue);

    public TemplateSyncPlanDtoValidator()
    {
        RuleFor(x => x.TargetTemplateVersion)
            .GreaterThan(0)
            .WithMessage("TargetTemplateVersion must be greater than 0.");

        RuleFor(x => x.BaseProvenanceVersion)
            .GreaterThanOrEqualTo(0)
            .WithMessage("BaseProvenanceVersion must be greater than or equal to 0.");

        RuleFor(x => x)
            .Must(x => x.GetTotalChangeCount() <= MaxChangeCount)
            .WithMessage($"Sync plan exceeds the maximum allowed change count of {MaxChangeCount}.");
    }
}
