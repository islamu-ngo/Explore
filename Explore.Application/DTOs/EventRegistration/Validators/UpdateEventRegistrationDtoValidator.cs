using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.EventRegistration.Validators
{
    public class UpdateEventRegistrationDtoValidator : AbstractValidator<UpdateEventRegistrationDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IEventSessionRepository _eventSessionRepository;
        private readonly IApprovalStatusRepository _approvalStatusRepository;

        public UpdateEventRegistrationDtoValidator(
            IUserRepository userRepository,
            IEventSessionRepository eventSessionRepository,
            IApprovalStatusRepository approvalStatusRepository)
        {
            _userRepository = userRepository;
            _eventSessionRepository = eventSessionRepository;
            _approvalStatusRepository = approvalStatusRepository;

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("{PropertyName} is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MustAsync(UserExists)
                .WithMessage("{PropertyName} not found");

            RuleFor(x => x.EventSessionId)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MustAsync(EventSessionExists)
                .WithMessage("{PropertyName} not found");

            RuleFor(x => x.ApprovalStatusId)
                .MustAsync(ApprovalStatusExists)
                .When(x => x.ApprovalStatusId.HasValue)
                .WithMessage("{PropertyName} not found");

            // TenantId is set by the handler from context, not by the client
            // No validation needed here
        }

        private async Task<bool> UserExists(Guid userId, CancellationToken cancellationToken)
        {
            return await _userRepository.Exists(userId);
        }

        private async Task<bool> EventSessionExists(Guid eventSessionId, CancellationToken cancellationToken)
        {
            return await _eventSessionRepository.Exists(eventSessionId);
        }

        private async Task<bool> ApprovalStatusExists(int? approvalStatusId, CancellationToken cancellationToken)
        {
            if (!approvalStatusId.HasValue) return true;
            return await _approvalStatusRepository.Exists(approvalStatusId.Value);
        }
    }
}
