// ABOUTME: Validates capacity pool payloads before the ticket catalog aggregate mutates.
// ABOUTME: Is manually constructed by capacity-pool command handlers.
using FluentValidation;

namespace Explore.Application.DTOs.EventTicketing.Validators;

public sealed class ManageEventCapacityPoolDtoValidator : AbstractValidator<ManageEventCapacityPoolDto>
{
    public ManageEventCapacityPoolDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.HoldDurationSeconds).GreaterThan(0);
    }
}
