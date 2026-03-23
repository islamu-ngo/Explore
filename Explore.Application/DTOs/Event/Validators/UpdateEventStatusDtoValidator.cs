// ABOUTME: Validates UpdateEventStatusDto — ensures EventStatusId is a valid EventStatus lookup.
// ABOUTME: Manually instantiated in the handler (no DI), receives IEventStatusRepository.

using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.Event.Validators;

public class UpdateEventStatusDtoValidator : AbstractValidator<UpdateEventStatusDto>
{
    public UpdateEventStatusDtoValidator(IEventStatusRepository eventStatusRepository)
    {
        RuleFor(p => p.EventStatusId)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MustAsync(async (id, ct) => await eventStatusRepository.Exists(id))
            .WithMessage("The specified EventStatusId does not exist.");
    }
}
