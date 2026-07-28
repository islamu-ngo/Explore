// ABOUTME: Validates ticket type payloads before the ticket catalog aggregate mutates.
// ABOUTME: Is manually constructed by ticket-type command handlers.
using FluentValidation;

namespace Explore.Application.DTOs.EventTicketing.Validators;

public sealed class ManageEventTicketTypeDtoValidator : AbstractValidator<ManageEventTicketTypeDto>
{
    public ManageEventTicketTypeDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Entitlements).NotEmpty();
    }
}
