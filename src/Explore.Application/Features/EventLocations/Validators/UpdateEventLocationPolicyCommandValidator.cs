// ABOUTME: Validates typed EventLocation policy input before any tracked mutation begins.
// ABOUTME: Rejects missing concurrency evidence, unknown enum values, and non-UTC reveal instants.

using Explore.Application.Features.EventLocations.Requests.Commands;
using Explore.Domain.Enums;
using FluentValidation;

namespace Explore.Application.Features.EventLocations.Validators;

public sealed class UpdateEventLocationPolicyCommandValidator
    : AbstractValidator<UpdateEventLocationPolicyCommand>
{
    public UpdateEventLocationPolicyCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.EventLocationId).NotEmpty();
        RuleFor(command => command.ExpectedConcurrencyStamp).NotEmpty();
        RuleFor(command => command.ExpectedPolicyVersion)
            .GreaterThan(0)
            .LessThan(int.MaxValue);
        RuleFor(command => command.SelectedFields)
            .Must(fields => (fields & ~EventLocationDisclosureFields.All) == 0)
            .WithMessage("SelectedFields contains an unknown EventLocation disclosure field.");
        RuleFor(command => command.FullDetailsAudience).IsInEnum();
        RuleFor(command => command.RevealFullDetailsFromUtc)
            .Must(value => !value.HasValue || value.Value.Kind == DateTimeKind.Utc)
            .WithMessage("RevealFullDetailsFromUtc must be UTC when provided.");
    }
}
