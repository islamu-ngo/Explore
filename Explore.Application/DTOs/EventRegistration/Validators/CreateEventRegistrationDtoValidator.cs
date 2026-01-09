using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.EventRegistration.Validators
{
    public class CreateEventRegistrationDtoValidator : AbstractValidator<CreateEventRegistrationDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IEventSessionRepository _eventSessionRepository;
        private readonly IApprovalStatusRepository _approvalStatusRepository;
        private readonly IEventRegistrationRepository _eventRegistrationRepository;

        public CreateEventRegistrationDtoValidator(
            IUserRepository userRepository,
            IEventSessionRepository eventSessionRepository,
            IApprovalStatusRepository approvalStatusRepository,
            IEventRegistrationRepository eventRegistrationRepository)
        {
            _userRepository = userRepository;
            _eventSessionRepository = eventSessionRepository;
            _approvalStatusRepository = approvalStatusRepository;
            _eventRegistrationRepository = eventRegistrationRepository;

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

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("{PropertyName} is required");

            RuleFor(x => x)
                .MustAsync(UserNotAlreadyRegistered)
                .WithMessage("User is already registered for this Event Session");
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

        private async Task<bool> UserNotAlreadyRegistered(CreateEventRegistrationDto dto, CancellationToken cancellationToken)
        {
            return !await _eventRegistrationRepository.IsUserRegisteredForSession(dto.UserId, dto.EventSessionId);
        }
    }
}
