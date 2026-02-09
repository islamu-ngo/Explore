using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.SyncState.Validators;

public class UpdateSyncStateDtoValidator : AbstractValidator<UpdateSyncStateDto>
{
    public UpdateSyncStateDtoValidator(ISyncStateRepository syncStateRepository)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required")
            .MustAsync(async (id, cancellation) => await syncStateRepository.Exists(id))
            .WithMessage("SyncState not found");

        RuleFor(x => x.Service)
            .NotEmpty().WithMessage("Service is required")
            .MaximumLength(500).WithMessage("Service must not exceed 500 characters");

        RuleFor(x => x.Cursor)
            .GreaterThanOrEqualTo(0).WithMessage("Cursor must be greater than or equal to 0");
    }
}
