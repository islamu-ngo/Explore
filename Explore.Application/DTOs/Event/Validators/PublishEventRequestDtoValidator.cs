using FluentValidation;

namespace Explore.Application.DTOs.Event.Validators;

public class PublishEventRequestDtoValidator : AbstractValidator<PublishEventRequestDto>
{
    public PublishEventRequestDtoValidator()
    {
        RuleFor(request => request.ExpectedConcurrencyStamp)
            .NotEmpty().WithMessage("ExpectedConcurrencyStamp is required.");
    }
}
