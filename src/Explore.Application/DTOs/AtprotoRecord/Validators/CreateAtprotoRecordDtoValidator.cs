using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.AtprotoRecord.Validators;

public class CreateAtprotoRecordDtoValidator : AbstractValidator<CreateAtprotoRecordDto>
{
    public CreateAtprotoRecordDtoValidator()
    {
        RuleFor(x => x.Did)
            .NotEmpty().WithMessage("Did is required")
            .MaximumLength(255).WithMessage("Did must not exceed 255 characters");

        RuleFor(x => x.Collection)
            .NotEmpty().WithMessage("Collection is required")
            .MaximumLength(500).WithMessage("Collection must not exceed 500 characters");

        RuleFor(x => x.RecordKey)
            .NotEmpty().WithMessage("Record key is required")
            .MaximumLength(500).WithMessage("Record key must not exceed 500 characters");

        RuleFor(x => x.Cid)
            .MaximumLength(255).WithMessage("Cid must not exceed 255 characters");

        RuleFor(x => x.Uri)
            .MaximumLength(500).WithMessage("Uri must not exceed 500 characters");
    }
}
