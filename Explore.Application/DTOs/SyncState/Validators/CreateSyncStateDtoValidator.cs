using FluentValidation;
using Explore.Application.Contracts.Persistence;

namespace Explore.Application.DTOs.SyncState.Validators
{
    public class CreateSyncStateDtoValidator : AbstractValidator<CreateSyncStateDto>
    {
        public CreateSyncStateDtoValidator()
        {
            RuleFor(x => x.Service)
                .NotEmpty().WithMessage("Service is required")
                .MaximumLength(500).WithMessage("Service must not exceed 500 characters");

            RuleFor(x => x.Cursor)
                .GreaterThanOrEqualTo(0).WithMessage("Cursor must be greater than or equal to 0");
        }
    }
}
