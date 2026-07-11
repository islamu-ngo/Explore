using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.IndexedDid.Validators;

public class UpdateIndexedDidDtoValidator : AbstractValidator<UpdateIndexedDidDto>
{
    public UpdateIndexedDidDtoValidator(IIndexedDidRepository indexedDidRepository)
    {
        RuleFor(x => x.Did)
            .NotEmpty().WithMessage("Did is required")
            .MustAsync(async (did, cancellation) => await indexedDidRepository.Exists(did))
            .WithMessage("IndexedDid not found");

        RuleFor(x => x.PdsHost)
            .NotEmpty().WithMessage("PDS host is required")
            .MaximumLength(500).WithMessage("PDS host must not exceed 500 characters");

        RuleFor(x => x.Handle)
            .MaximumLength(255).WithMessage("Handle must not exceed 255 characters");

        RuleFor(x => x.SigningKey)
            .MaximumLength(500).WithMessage("Signing key must not exceed 500 characters");

        RuleFor(x => x.LastIndexedAt)
            .NotEmpty().WithMessage("Last indexed at is required");
    }
}
