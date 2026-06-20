// ABOUTME: Handler for the intent-first registration flow - creates an EventRegistrationIntent parent and its EventRegistration child access rows atomically.
// ABOUTME: Enforces organizer policy via RegistrationPolicyRules, derives child sessions from scope, writes inside a serializable transaction.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.DTOs.EventRegistration.Validators;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.EventRegistrations.Handlers.Commands;

public class CreateEventRegistrationCommandHandler : IRequestHandler<CreateEventRegistrationCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRegistrationIntentRepository _intentRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IApprovalStatusRepository _approvalStatusRepository;
    private readonly ITenantContext _tenantContext;
    private readonly BusinessMetrics _metrics;
    private readonly IContactShareConsentService _consentService;
    private readonly IEventLifecycleEmailOutboxFactory _emailOutboxFactory;
    private readonly ILogger<CreateEventRegistrationCommandHandler> _logger;

    public CreateEventRegistrationCommandHandler(
        IEventRegistrationIntentRepository intentRepository,
        IEventRepository eventRepository,
        IUserRepository userRepository,
        IEventDayRepository eventDayRepository,
        IEventSessionRepository eventSessionRepository,
        IApprovalStatusRepository approvalStatusRepository,
        ITenantContext tenantContext,
        BusinessMetrics metrics,
        IContactShareConsentService consentService,
        IEventLifecycleEmailOutboxFactory emailOutboxFactory,
        ILogger<CreateEventRegistrationCommandHandler> logger)
    {
        _intentRepository = intentRepository;
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _eventDayRepository = eventDayRepository;
        _eventSessionRepository = eventSessionRepository;
        _approvalStatusRepository = approvalStatusRepository;
        _tenantContext = tenantContext;
        _metrics = metrics;
        _consentService = consentService;
        _emailOutboxFactory = emailOutboxFactory;
        _logger = logger;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventRegistrationCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateEventRegistrationDtoValidator(
            _eventRepository,
            _userRepository,
            _eventDayRepository,
            _eventSessionRepository,
            _approvalStatusRepository);
        var validationResult = await validator.ValidateAsync(request.EventRegistrationDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event Registration failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var dto = request.EventRegistrationDto;
        var parentEvent = await _eventRepository.GetById(dto.EventId);
        if (parentEvent is null)
        {
            response.Success = false;
            response.Message = "Event not found in the current tenant.";
            return response;
        }

        var user = await _userRepository.GetById(dto.UserId);
        if (user is null)
        {
            response.Success = false;
            response.Message = "User not found.";
            return response;
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            response.Success = false;
            response.Message = "User email address is required for registration confirmation.";
            return response;
        }

        // Short-circuit idempotency: if the user already has the same intent, return its id.
        var existing = await _intentRepository.FindExistingAsync(
            dto.EventId,
            dto.UserId,
            dto.RegistrationScopeId,
            dto.SelectedEventDayId,
            cancellationToken);
        if (existing is not null)
        {
            response.Success = true;
            response.Id = existing.Id;
            response.Message = "Event Registration already exists.";
            return response;
        }

        // Derive child session access rows from the scope.
        var childSessionIds = await ResolveChildSessionsAsync(dto, cancellationToken);
        if (childSessionIds.Count == 0)
        {
            response.Success = false;
            response.Message = "Event Registration failed.";
            response.Errors = new List<string>
            {
                "Cannot create a registration with zero session access rows - the event has no sessions matching the requested scope."
            };
            return response;
        }

        var tenantId = parentEvent.TenantId;

        var intent = new EventRegistrationIntent
        {
            Id = Guid.CreateVersion7(),
            EventId = dto.EventId,
            Event = null!,
            UserId = dto.UserId,
            User = null!,
            RegistrationScopeId = dto.RegistrationScopeId,
            RegistrationScope = null!,
            SelectedEventDayId = dto.SelectedEventDayId,
            RegistrationPolicySnapshotId = parentEvent.RegistrationPolicyId,
            ApprovalStatusId = (int)ApprovalStatusEnum.Approved,
            TenantId = tenantId,
            Tenant = null!
        };

        var childRows = childSessionIds
            .Select(sessionId => new EventRegistration
            {
                EventId = dto.EventId,
                Event = null!,
                UserId = dto.UserId,
                User = null!,
                EventSessionId = sessionId,
                EventSession = null!,
                ApprovalStatusId = (int)ApprovalStatusEnum.Approved,
                TenantId = tenantId,
                Tenant = null!
            })
            .ToList();

        var emailDispatchOutbox = _emailOutboxFactory.CreateRegistrationConfirmation(
            tenantId,
            dto.UserId,
            dto.EventId,
            intent.Id,
            user.Email,
            parentEvent.Title);

        var creationResult = await _intentRepository.CreateWithChildrenAndCapacityAsync(
            intent,
            childRows,
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Waitlisted,
            cancellationToken,
            emailDispatchOutbox);
        var created = creationResult.Intent;

        if (dto.ShareEmailWithOrganizer)
        {
            try
            {
                await _consentService.ProcessRegistrationConsent(
                    tenantId,
                    dto.UserId,
                    dto.EventId,
                    created.Id,
                    dto.ShareEmailWithOrganizer,
                    dto.ConsentTextAcknowledged,
                    dto.ConsentUiVersion);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to process contact share consent for registration {RegistrationId}; registration itself succeeded.",
                    created.Id);
            }
        }

        response.Success = true;
        response.Id = created.Id;
        response.Message = creationResult.HasWaitlistedSessions
            ? "Event Registration added to the waitlist."
            : "Event Registration created successfully.";
        _metrics.RecordRegistrationCreated(tenantId.ToString());

        return response;
    }

    private async Task<List<Guid>> ResolveChildSessionsAsync(CreateEventRegistrationDto dto, CancellationToken cancellationToken)
    {
        var scope = (RegistrationScopeEnum)dto.RegistrationScopeId;

        if (scope == RegistrationScopeEnum.SessionSelection)
        {
            return dto.SelectedSessionIds?.Distinct().ToList() ?? [];
        }

        var allSessions = await _eventSessionRepository.GetSessionsByEvent(dto.EventId);

        if (scope == RegistrationScopeEnum.Event)
        {
            return allSessions.Select(s => s.Id).ToList();
        }

        if (scope == RegistrationScopeEnum.Day && dto.SelectedEventDayId.HasValue)
        {
            return allSessions
                .Where(s => s.EventDayId == dto.SelectedEventDayId.Value)
                .Select(s => s.Id)
                .ToList();
        }

        return new List<Guid>();
    }
}
