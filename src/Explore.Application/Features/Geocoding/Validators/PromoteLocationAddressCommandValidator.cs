// ABOUTME: Validates the narrow address-promotion target and optimistic concurrency evidence.
// ABOUTME: Leaves tenant, actor, provenance, visibility, and authorization to trusted server context.

using Explore.Application.Features.Geocoding.Requests.Commands;
using FluentValidation;

namespace Explore.Application.Features.Geocoding.Validators;

public sealed class PromoteLocationAddressCommandValidator
    : AbstractValidator<PromoteLocationAddressCommand>
{
    public PromoteLocationAddressCommandValidator()
    {
        RuleFor(command => command.LocationId).NotEmpty();
        RuleFor(command => command.ExpectedConcurrencyStamp).NotEmpty();
    }
}
