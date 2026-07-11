// ABOUTME: Validator for the intent-first CreateEventRegistrationDto - enforces organizer EventRegistrationPolicy fail-fast.
// ABOUTME: Validates event + user existence, selected day membership, selected session membership, and scope-policy compatibility via RegistrationPolicyRules.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Registration;
using FluentValidation;

namespace Explore.Application.DTOs.EventRegistration.Validators;

public class CreateEventRegistrationDtoValidator : AbstractValidator<CreateEventRegistrationDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IApprovalStatusRepository _approvalStatusRepository;

    public CreateEventRegistrationDtoValidator(
        IEventRepository eventRepository,
        IUserRepository userRepository,
        IEventDayRepository eventDayRepository,
        IEventSessionRepository eventSessionRepository,
        IApprovalStatusRepository approvalStatusRepository)
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _eventDayRepository = eventDayRepository;
        _eventSessionRepository = eventSessionRepository;
        _approvalStatusRepository = approvalStatusRepository;

        RuleFor(p => p.EventId)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MustAsync(async (id, ct) => await _eventRepository.Exists(id))
            .WithMessage("{PropertyName} does not exist.");

        RuleFor(p => p.UserId)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MustAsync(async (id, ct) => await _userRepository.Exists(id))
            .WithMessage("{PropertyName} does not exist.");

        RuleFor(p => p.RegistrationScopeId)
            .Must(scopeId => Enum.IsDefined(typeof(RegistrationScopeEnum), scopeId))
            .WithMessage("{PropertyName} must be a known registration scope.");

        RuleFor(p => p.ApprovalStatusId)
            .MustAsync(async (id, ct) =>
            {
                if (!id.HasValue) return true;
                return await _approvalStatusRepository.Exists(id.Value);
            })
            .When(p => p.ApprovalStatusId.HasValue)
            .WithMessage("{PropertyName} does not exist.");

        // Day scope: a non-empty SelectedEventDayId that belongs to the event
        RuleFor(p => p)
            .MustAsync(async (dto, ct) =>
            {
                if (dto.RegistrationScopeId != (int)RegistrationScopeEnum.Day) return true;
                if (!dto.SelectedEventDayId.HasValue) return false;
                return await _eventDayRepository.BelongsToEventAsync(dto.SelectedEventDayId.Value, dto.EventId, ct);
            })
            .When(p => p.RegistrationScopeId == (int)RegistrationScopeEnum.Day)
            .WithMessage("SelectedEventDayId must reference a day belonging to the event when scope is Day.");

        // Session-selection scope: non-empty list and every session belongs to the event
        RuleFor(p => p.SelectedSessionIds)
            .NotEmpty()
            .WithMessage("SelectedSessionIds must contain at least one session when scope is SessionSelection.")
            .When(p => p.RegistrationScopeId == (int)RegistrationScopeEnum.SessionSelection);

        RuleFor(p => p)
            .MustAsync(AllSelectedSessionsBelongToEvent)
            .When(p => p.RegistrationScopeId == (int)RegistrationScopeEnum.SessionSelection && p.SelectedSessionIds?.Count > 0)
            .WithMessage("All SelectedSessionIds must belong to the supplied EventId.");

        // Organizer policy enforcement (fail-fast).
        RuleFor(p => p)
            .MustAsync(ScopeAllowedByPolicy)
            .WithMessage("The requested registration scope is not permitted by this event's registration policy.");
    }

    private async Task<bool> AllSelectedSessionsBelongToEvent(CreateEventRegistrationDto dto, CancellationToken ct)
    {
        var sessions = await _eventSessionRepository.GetSessionsByEvent(dto.EventId);
        var validSessionIds = sessions.Select(s => s.Id).ToHashSet();
        return (dto.SelectedSessionIds ?? Array.Empty<Guid>()).All(validSessionIds.Contains);
    }

    private async Task<bool> ScopeAllowedByPolicy(CreateEventRegistrationDto dto, CancellationToken ct)
    {
        var @event = await _eventRepository.GetById(dto.EventId);
        if (@event is null) return true; // EventId existence rule will fail separately
        return RegistrationPolicyRules.IsScopeAllowed(@event.RegistrationPolicyId, dto.RegistrationScopeId);
    }
}
