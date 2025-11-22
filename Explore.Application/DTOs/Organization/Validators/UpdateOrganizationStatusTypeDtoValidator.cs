using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.Organization.Validators
{
    public class UpdateOrganizationStatusTypeDtoValidator : AbstractValidator<UpdateOrganizationStatusTypeDto>
    {
        private readonly IStatusTypeRepository _statusTypeRepository;

        public UpdateOrganizationStatusTypeDtoValidator(IStatusTypeRepository statusTypeRepository)
        {
            _statusTypeRepository = statusTypeRepository;

            RuleFor(p => p.StatusTypeId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MustAsync(async (id, cancellation) =>
                {
                    var statusTypeExists = await _statusTypeRepository.Exists(id);
                    return statusTypeExists;
                }).WithMessage("{PropertyName} does not exist.");
        }
    }
}
