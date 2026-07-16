// ABOUTME: Handler for the intent-first registration flow - creates an EventRegistrationIntent parent and its EventRegistration child access rows atomically.
// ABOUTME: Enforces organizer policy via RegistrationPolicyRules, derives child sessions from scope, writes inside a serializable transaction.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.DTOs.EventRegistration.Validators;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Registration;
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
    private readonly IListmonkRegistrationSyncOutboxFactory _listmonkOutboxFactory;
    private readonly IRegistrationNotificationDeliveryService _notificationDeliveryService;
    private readonly IWebhookEventPublisher _webhookPublisher;
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
        IListmonkRegistrationSyncOutboxFactory listmonkOutboxFactory,
        IRegistrationNotificationDeliveryService notificationDeliveryService,
        IWebhookEventPublisher webhookPublisher,
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
        _listmonkOutboxFactory = listmonkOutboxFactory;
        _notificationDeliveryService = notificationDeliveryService;
        _webhookPublisher = webhookPublisher;
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

        var emailResolution = _notificationDeliveryService.ResolveRegistrationConfirmationEmail(user);

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
        var childSessions = await ResolveChildSessionsAsync(dto);
        if (childSessions.Count == 0)
        {
            response.Success = false;
            response.Message = "Event Registration failed.";
            response.Errors = new List<string>
            {
                "Cannot create a registration with zero session access rows - the event has no sessions matching the requested scope."
            };
            return response;
        }

        var initialApprovalStatus = RegistrationPolicyRules.ResolveInitialApprovalStatus(
            childSessions.Select(session => session.RegistrationModeId));
        if (initialApprovalStatus is null)
        {
            response.Success = false;
            response.Message = "Event Registration failed.";
            response.Errors = ["Registration is not currently available for every selected session."];
            return response;
        }

        var initialApprovalStatusId = (int)initialApprovalStatus.Value;

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
            ApprovalStatusId = initialApprovalStatusId,
            TenantId = tenantId,
            Tenant = null!
        };

        var childRows = childSessions
            .Select(session => new EventRegistration
            {
                EventId = dto.EventId,
                Event = null!,
                UserId = dto.UserId,
                User = null!,
                EventSessionId = session.Id,
                EventSession = null!,
                ApprovalStatusId = initialApprovalStatusId,
                TenantId = tenantId,
                Tenant = null!
            })
            .ToList();

        var emailDispatchOutbox = emailResolution.HasVerifiedEmail
            ? _emailOutboxFactory.CreateRegistrationConfirmation(
                tenantId,
                dto.UserId,
                dto.EventId,
                intent.Id,
                emailResolution.Email!,
                parentEvent.Title)
            : null;
        var listmonkSyncOutbox = await _listmonkOutboxFactory.CreateForRegistrationAsync(
            parentEvent,
            user,
            dto,
            intent.Id,
            cancellationToken);

        var creationResult = await _intentRepository.CreateWithChildrenAndCapacityAsync(
            intent,
            childRows,
            initialApprovalStatusId,
            (int)ApprovalStatusEnum.Waitlisted,
            cancellationToken,
            emailDispatchOutbox,
            listmonkSyncOutbox);
        var created = creationResult.Intent;

        if (creationResult.WasExisting)
        {
            response.Success = true;
            response.Id = created.Id;
            response.Message = "Event Registration already exists.";
            return response;
        }

        if (emailDispatchOutbox is not null)
        {
            await _emailOutboxFactory.EnqueueNotificationIntentAsync(emailDispatchOutbox, cancellationToken);
        }
        else
        {
            await _notificationDeliveryService.CreateRegistrationConfirmationFallbackAsync(
                user,
                tenantId,
                dto.EventId,
                created.Id,
                parentEvent.Title,
                cancellationToken);
        }

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

        await PublishRegistrationCreatedWebhookAsync(
            tenantId,
            dto,
            user,
            created.Id,
            creationResult.HasWaitlistedSessions,
            created.ApprovalStatusId,
            cancellationToken);

        response.Success = true;
        response.Id = created.Id;
        response.Message = creationResult.HasWaitlistedSessions
            ? "Event Registration added to the waitlist."
            : created.ApprovalStatusId == (int)ApprovalStatusEnum.Pending
                ? "Event Registration submitted for approval."
                : "Event Registration created successfully.";
        _metrics.RecordRegistrationCreated(tenantId.ToString());

        return response;
    }

    private async Task PublishRegistrationCreatedWebhookAsync(
        Guid tenantId,
        CreateEventRegistrationDto dto,
        User user,
        Guid registrationIntentId,
        bool hasWaitlistedSessions,
        int? approvalStatusId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _webhookPublisher.PublishAsync(
                new WebhookEventBuildContext(
                    Guid.CreateVersion7(),
                    tenantId,
                    WebhookEventNames.RegistrationCreated,
                    registrationIntentId.ToString(),
                    nameof(EventRegistrationIntent),
                    registrationIntentId,
                    DateTimeOffset.UtcNow,
                    BuildRegistrationCreatedWebhookData(
                        dto,
                        user,
                        registrationIntentId,
                        hasWaitlistedSessions,
                        approvalStatusId)),
                cancellationToken);

            if (!result.Succeeded && !result.Skipped)
            {
                _logger.LogWarning(
                    "Failed to publish registration.created webhook for registration {RegistrationId}: {FailureCategory} {SafeDetail}",
                    registrationIntentId,
                    result.FailureCategory,
                    result.SafeDetail);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Failed to publish registration.created webhook for registration {RegistrationId}; registration itself succeeded.",
                registrationIntentId);
        }
    }

    private static IReadOnlyDictionary<string, object?> BuildRegistrationCreatedWebhookData(
        CreateEventRegistrationDto dto,
        User user,
        Guid registrationIntentId,
        bool hasWaitlistedSessions,
        int? approvalStatusId)
    {
        var data = new Dictionary<string, object?>
        {
            ["registrationId"] = registrationIntentId.ToString(),
            ["eventId"] = dto.EventId.ToString(),
            ["status"] = hasWaitlistedSessions
                ? ApprovalStatusEnum.Waitlisted.ToString()
                : Enum.IsDefined(typeof(ApprovalStatusEnum), approvalStatusId ?? 0)
                    ? ((ApprovalStatusEnum)approvalStatusId!.Value).ToString()
                    : ApprovalStatusEnum.Rejected.ToString(),
            ["consentToEmailShare"] = dto.ShareEmailWithOrganizer
        };

        if (!dto.ShareEmailWithOrganizer)
        {
            return data;
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            data["attendeeEmail"] = user.Email;
        }

        if (!string.IsNullOrWhiteSpace(user.FirstName))
        {
            data["attendeeFirstName"] = user.FirstName;
        }

        if (!string.IsNullOrWhiteSpace(user.LastName))
        {
            data["attendeeLastName"] = user.LastName;
        }

        return data;
    }

    private async Task<List<EventSession>> ResolveChildSessionsAsync(CreateEventRegistrationDto dto)
    {
        var scope = (RegistrationScopeEnum)dto.RegistrationScopeId;
        var allSessions = await _eventSessionRepository.GetSessionsByEvent(dto.EventId);

        if (scope == RegistrationScopeEnum.SessionSelection)
        {
            var selectedSessionIds = dto.SelectedSessionIds?.ToHashSet() ?? [];
            var selectedSessions = allSessions
                .Where(session => selectedSessionIds.Contains(session.Id))
                .ToList();
            return selectedSessions.Count == selectedSessionIds.Count ? selectedSessions : [];
        }

        if (scope == RegistrationScopeEnum.Event)
        {
            return allSessions;
        }

        if (scope == RegistrationScopeEnum.Day && dto.SelectedEventDayId.HasValue)
        {
            return allSessions
                .Where(s => s.EventDayId == dto.SelectedEventDayId.Value)
                .ToList();
        }

        return [];
    }
}
