using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.Organization.Validators
{
    public class UpdateOrganizationApprovalStatusDtoValidator : AbstractValidator<UpdateOrganizationApprovalStatusDto>
    {
        private readonly IApprovalStatusRepository _statusTypeRepository;

        public UpdateOrganizationApprovalStatusDtoValidator(IApprovalStatusRepository statusTypeRepository)
        {
            _statusTypeRepository = statusTypeRepository;

            RuleFor(p => p.ApprovalStatusId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MustAsync(async (id, cancellation) =>
                {
                    var approvalStatusExists = await _statusTypeRepository.Exists(id);
                    return approvalStatusExists;
                }).WithMessage("{PropertyName} does not exist.");
        }
    }
}
