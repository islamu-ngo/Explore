// ABOUTME: Tests intent-first event registration DTO validation rules.
// ABOUTME: Covers organizer policy, day membership, and selected-session membership checks.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.DTOs.EventRegistration.Validators;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.DTOs.EventRegistration.Validators;

public sealed class CreateEventRegistrationDtoValidatorTests
{
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEventDayRepository _eventDayRepository = Substitute.For<IEventDayRepository>();
    private readonly IEventSessionRepository _eventSessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly IApprovalStatusRepository _approvalStatusRepository = Substitute.For<IApprovalStatusRepository>();
    private readonly CreateEventRegistrationDtoValidator _validator;

    public CreateEventRegistrationDtoValidatorTests()
    {
        _validator = new CreateEventRegistrationDtoValidator(
            _eventRepository,
            _userRepository,
            _eventDayRepository,
            _eventSessionRepository,
            _approvalStatusRepository);
    }

    [Test]
    public async Task Validate_WithAllowedEventScope_ReturnsValid()
    {
        var dto = CreateDto(RegistrationScopeEnum.Event);
        SetupValidBaseLookups(dto, EventRegistrationPolicyEnum.Flexible);

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithEventScopeAndNullSelectedSessionIds_ReturnsValid()
    {
        var dto = CreateDto(RegistrationScopeEnum.Event);
        dto.SelectedSessionIds = null;
        SetupValidBaseLookups(dto, EventRegistrationPolicyEnum.WholeEventOnly);

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithSessionScopeAndNullSelectedSessionIds_ReturnsSessionSelectionError()
    {
        var dto = CreateDto(RegistrationScopeEnum.SessionSelection);
        dto.SelectedSessionIds = null;
        SetupValidBaseLookups(dto, EventRegistrationPolicyEnum.SessionSelectionOnly);

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("SelectedSessionIds must contain at least one session when scope is SessionSelection.");
    }

    [Test]
    public async Task Validate_WhenScopeIsNotAllowedByEventPolicy_ReturnsPolicyError()
    {
        var dto = CreateDto(RegistrationScopeEnum.SessionSelection);
        var selectedSessionId = Guid.NewGuid();
        dto.SelectedSessionIds = [selectedSessionId];
        SetupValidBaseLookups(dto, EventRegistrationPolicyEnum.WholeEventOnly);
        _eventSessionRepository.GetSessionsByEvent(dto.EventId).Returns([CreateSession(dto.EventId, selectedSessionId)]);

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("The requested registration scope is not permitted by this event's registration policy.");
    }

    [Test]
    public async Task Validate_WhenDayScopeReferencesForeignDay_ReturnsDayMembershipError()
    {
        var dto = CreateDto(RegistrationScopeEnum.Day);
        dto.SelectedEventDayId = Guid.NewGuid();
        SetupValidBaseLookups(dto, EventRegistrationPolicyEnum.Flexible);
        _eventDayRepository.BelongsToEventAsync(dto.SelectedEventDayId.Value, dto.EventId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("SelectedEventDayId must reference a day belonging to the event when scope is Day.");
    }

    [Test]
    public async Task Validate_WhenSelectedSessionDoesNotBelongToEvent_ReturnsSessionMembershipError()
    {
        var dto = CreateDto(RegistrationScopeEnum.SessionSelection);
        dto.SelectedSessionIds = [Guid.NewGuid()];
        SetupValidBaseLookups(dto, EventRegistrationPolicyEnum.Flexible);
        _eventSessionRepository.GetSessionsByEvent(dto.EventId).Returns([CreateSession(dto.EventId, Guid.NewGuid())]);

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("All SelectedSessionIds must belong to the supplied EventId.");
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments((int)RegistrationScopeEnum.Event)]
    [Arguments((int)RegistrationScopeEnum.Day)]
    [Arguments((int)RegistrationScopeEnum.SessionSelection)]
    public async Task Validate_WithNullApprovalStatus_AcceptsEveryWellFormedScope(int scopeId)
    {
        var scope = (RegistrationScopeEnum)scopeId;
        var dto = CreateDto(scope);
        dto.ApprovalStatusId = null;
        SetupValidBaseLookups(dto, EventRegistrationPolicyEnum.Flexible);

        if (scope == RegistrationScopeEnum.Day)
        {
            dto.SelectedEventDayId = Guid.NewGuid();
            _eventDayRepository.BelongsToEventAsync(
                    dto.SelectedEventDayId.Value,
                    dto.EventId,
                    Arg.Any<CancellationToken>())
                .Returns(true);
        }
        else if (scope == RegistrationScopeEnum.SessionSelection)
        {
            var sessionId = Guid.NewGuid();
            dto.SelectedSessionIds = [sessionId];
            _eventSessionRepository.GetSessionsByEvent(dto.EventId)
                .Returns([CreateSession(dto.EventId, sessionId)]);
        }

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsTrue();
        await _approvalStatusRepository.DidNotReceive().Exists(Arg.Any<int>());
    }

    private void SetupValidBaseLookups(CreateEventRegistrationDto dto, EventRegistrationPolicyEnum policy)
    {
        _eventRepository.Exists(dto.EventId).Returns(true);
        _userRepository.Exists(dto.UserId).Returns(true);
        _eventRepository.GetById(dto.EventId).Returns(CreateEvent(dto.EventId, policy));
        if (dto.ApprovalStatusId.HasValue)
        {
            _approvalStatusRepository.Exists(dto.ApprovalStatusId.Value).Returns(true);
        }
    }

    private static CreateEventRegistrationDto CreateDto(RegistrationScopeEnum scope) => new()
    {
        EventId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        RegistrationScopeId = (int)scope,
        ApprovalStatusId = 1
    };

    private static Explore.Domain.Event CreateEvent(Guid eventId, EventRegistrationPolicyEnum policy) => new()
    {
        Id = eventId,
        Title = "Registration Validator Event",
        Actor = null!,
        TenantId = Guid.NewGuid(),
        Tenant = null!,
        VisibilityTypeId = (int)VisibilityTypeEnum.Public,
        VisibilityType = null!,
        EventStatusId = (int)EventStatusEnum.Published,
        EventStatus = null!,
        EventFormatId = (int)EventFormatEnum.Local,
        EventFormat = null!,
        RegistrationPolicyId = (int)policy
    };

    private static EventSession CreateSession(Guid eventId, Guid sessionId) => new()
    {
        Id = sessionId,
        EventId = eventId,
        Event = null!,
        TenantId = Guid.NewGuid(),
        Tenant = null!,
        StartTime = DateTimeOffset.UtcNow.AddDays(1),
        EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(1)
    };
}
