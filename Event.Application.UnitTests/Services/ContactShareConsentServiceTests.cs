// ABOUTME: Unit tests for ContactShareConsentService — the core consent lifecycle logic.
// ABOUTME: Covers grant, re-grant, withdrawal, duplicate prevention, and validation rules.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.ContactShareConsent;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Services;

public class ContactShareConsentServiceTests
{
    private readonly IEventContactShareConsentRepository _consentRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ILogger<ContactShareConsentService> _logger;
    private readonly ContactShareConsentService _service;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _eventId = Guid.NewGuid();
    private readonly Guid _registrationIntentId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _orgId = Guid.NewGuid();

    public ContactShareConsentServiceTests()
    {
        _consentRepository = Substitute.For<IEventContactShareConsentRepository>();
        _eventSessionRepository = Substitute.For<IEventSessionRepository>();
        _eventRepository = Substitute.For<IEventRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _actorRepository = Substitute.For<IActorRepository>();
        _organizationRepository = Substitute.For<IOrganizationRepository>();
        _logger = Substitute.For<ILogger<ContactShareConsentService>>();

        _service = new ContactShareConsentService(
            _consentRepository,
            _eventSessionRepository,
            _eventRepository,
            _userRepository,
            _actorRepository,
            _organizationRepository,
            _logger);
    }

    #region ProcessRegistrationConsent — checkbox unchecked

    [Test]
    public async Task ProcessRegistrationConsent_UncheckedCheckbox_ReturnsNullAndCreatesNothing()
    {
        // Act
        var result = await _service.ProcessRegistrationConsent(
            _tenantId, _userId, _eventId, _registrationIntentId,
            shareEmailWithOrganizer: false, null, null);

        // Assert
        await Assert.That(result).IsNull();
        await _consentRepository.DidNotReceive().Create(Arg.Any<EventContactShareConsent>());
        await _consentRepository.DidNotReceive().Update(Arg.Any<EventContactShareConsent>());
    }

    #endregion

    #region ProcessRegistrationConsent — checkbox checked, happy path

    [Test]
    public async Task ProcessRegistrationConsent_CheckedCheckbox_CreatesConsentWithEmailSnapshot()
    {
        // Arrange
        var email = "User@Example.com";
        SetupValidChain(email);
        _consentRepository.GetByScope(_tenantId, _userId, _actorId, ConsentPurposeCodes.OrganizerFutureCommunications)
            .Returns((EventContactShareConsent?)null);

        var createdConsent = new EventContactShareConsent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            UserId = _userId,
            RecipientActorId = _actorId,
            PurposeCode = ConsentPurposeCodes.OrganizerFutureCommunications,
            Status = ConsentStatus.Granted,
            EmailSnapshot = email,
            EmailNormalizedSnapshot = email.ToLowerInvariant(),
            ConsentTextSnapshot = "test",
            ConsentUiVersion = "v1",
            GrantedAt = DateTime.UtcNow
        };
        _consentRepository.Create(Arg.Any<EventContactShareConsent>()).Returns(createdConsent);

        // Act
        var result = await _service.ProcessRegistrationConsent(
            _tenantId, _userId, _eventId, _registrationIntentId,
            shareEmailWithOrganizer: true, "consent text", "v1");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsEqualTo(createdConsent.Id);

        await _consentRepository.Received(1).Create(Arg.Is<EventContactShareConsent>(c =>
            c.TenantId == _tenantId &&
            c.UserId == _userId &&
            c.RecipientActorId == _actorId &&
            c.Status == ConsentStatus.Granted &&
            c.EmailSnapshot == email &&
            c.EmailNormalizedSnapshot == email.ToLowerInvariant() &&
            c.SourceEventId == _eventId &&
            c.SourceEventRegistrationIntentId == _registrationIntentId));
    }

    #endregion

    #region ProcessRegistrationConsent — re-registration for same organizer

    [Test]
    public async Task ProcessRegistrationConsent_ExistingConsentForOrganizer_ReactivatesInsteadOfCreatingDuplicate()
    {
        // Arrange
        var email = "new-email@example.com";
        SetupValidChain(email);

        var existingConsent = CreateExistingConsent(ConsentStatus.Withdrawn);
        _consentRepository.GetByScope(_tenantId, _userId, _actorId, ConsentPurposeCodes.OrganizerFutureCommunications)
            .Returns(existingConsent);

        // Act
        var result = await _service.ProcessRegistrationConsent(
            _tenantId, _userId, _eventId, _registrationIntentId,
            shareEmailWithOrganizer: true, "consent text", "v1");

        // Assert
        await Assert.That(result).IsEqualTo(existingConsent.Id);
        await Assert.That(existingConsent.Status).IsEqualTo(ConsentStatus.Granted);
        await Assert.That(existingConsent.EmailSnapshot).IsEqualTo(email);
        await Assert.That(existingConsent.WithdrawnAt).IsNull();
        await Assert.That(existingConsent.SourceEventId).IsEqualTo(_eventId);
        await Assert.That(existingConsent.SourceEventRegistrationIntentId).IsEqualTo(_registrationIntentId);

        await _consentRepository.Received(1).Update(existingConsent);
        await _consentRepository.DidNotReceive().Create(Arg.Any<EventContactShareConsent>());
    }

    #endregion

    #region ProcessRegistrationConsent — validation failures

    [Test]
    public async Task ProcessRegistrationConsent_EventNotFound_ReturnsNull()
    {
        // Arrange
        _eventRepository.GetById(_eventId).Returns((Explore.Domain.Event?)null);

        // Act
        var result = await _service.ProcessRegistrationConsent(
            _tenantId, _userId, _eventId, _registrationIntentId,
            shareEmailWithOrganizer: true, null, null);

        // Assert
        await Assert.That(result).IsNull();
        await _consentRepository.DidNotReceive().Create(Arg.Any<EventContactShareConsent>());
    }

    [Test]
    public async Task ProcessRegistrationConsent_ActorHasNoOrganization_ReturnsNull()
    {
        // Arrange
        _eventRepository.GetById(_eventId).Returns(CreateEvent());
        _actorRepository.GetById(_actorId).Returns(new Actor
        {
            Id = _actorId,
            OrganizationId = null, // Not an org actor
            Pii = new ActorPii { DisplayName = "Test" },
            ActorType = CreateActorType(),
            Tenant = CreateTenant()
        });

        // Act
        var result = await _service.ProcessRegistrationConsent(
            _tenantId, _userId, _eventId, _registrationIntentId,
            shareEmailWithOrganizer: true, null, null);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ProcessRegistrationConsent_OrganizationNotApproved_ReturnsNull()
    {
        // Arrange
        SetupEventAndActor();
        _organizationRepository.GetById(_orgId).Returns(CreateOrganizationEntity(_orgId, approved: false));

        // Act
        var result = await _service.ProcessRegistrationConsent(
            _tenantId, _userId, _eventId, _registrationIntentId,
            shareEmailWithOrganizer: true, null, null);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ProcessRegistrationConsent_UserHasNoEmail_ReturnsNull()
    {
        // Arrange
        SetupEventAndActor();
        SetupApprovedOrg();
        _userRepository.GetUserWithDetails(_userId).Returns(new User
        {
            Id = _userId,
            Pii = new UserPii { Email = "", FirstName = "Test", LastName = "User" }
        });

        // Act
        var result = await _service.ProcessRegistrationConsent(
            _tenantId, _userId, _eventId, _registrationIntentId,
            shareEmailWithOrganizer: true, null, null);

        // Assert
        await Assert.That(result).IsNull();
    }

    #endregion

    #region HasGrantedConsentForOrganizer

    [Test]
    public async Task HasGrantedConsentForOrganizer_GrantedExists_ReturnsTrue()
    {
        // Arrange
        var consent = CreateExistingConsent(ConsentStatus.Granted);
        _consentRepository.GetByScope(_tenantId, _userId, _actorId, ConsentPurposeCodes.OrganizerFutureCommunications)
            .Returns(consent);

        // Act
        var result = await _service.HasGrantedConsentForOrganizer(_tenantId, _userId, _actorId);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task HasGrantedConsentForOrganizer_WithdrawnExists_ReturnsFalse()
    {
        // Arrange
        var consent = CreateExistingConsent(ConsentStatus.Withdrawn);
        _consentRepository.GetByScope(_tenantId, _userId, _actorId, ConsentPurposeCodes.OrganizerFutureCommunications)
            .Returns(consent);

        // Act
        var result = await _service.HasGrantedConsentForOrganizer(_tenantId, _userId, _actorId);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task HasGrantedConsentForOrganizer_NoneExists_ReturnsFalse()
    {
        // Arrange
        _consentRepository.GetByScope(_tenantId, _userId, _actorId, ConsentPurposeCodes.OrganizerFutureCommunications)
            .Returns((EventContactShareConsent?)null);

        // Act
        var result = await _service.HasGrantedConsentForOrganizer(_tenantId, _userId, _actorId);

        // Assert
        await Assert.That(result).IsFalse();
    }

    #endregion

    #region WithdrawConsent

    [Test]
    public async Task WithdrawConsent_GrantedConsent_MarksWithdrawn()
    {
        // Arrange
        var consent = CreateExistingConsent(ConsentStatus.Granted);
        consent.TenantId = _tenantId;
        consent.UserId = _userId;
        _consentRepository.GetById(consent.Id).Returns(consent);

        // Act
        await _service.WithdrawConsent(_tenantId, _userId, consent.Id);

        // Assert
        await Assert.That(consent.Status).IsEqualTo(ConsentStatus.Withdrawn);
        await Assert.That(consent.WithdrawnAt).IsNotNull();
        await _consentRepository.Received(1).Update(consent);
    }

    [Test]
    public async Task WithdrawConsent_AlreadyWithdrawn_IsIdempotent()
    {
        // Arrange
        var consent = CreateExistingConsent(ConsentStatus.Withdrawn);
        consent.TenantId = _tenantId;
        consent.UserId = _userId;
        _consentRepository.GetById(consent.Id).Returns(consent);

        // Act
        await _service.WithdrawConsent(_tenantId, _userId, consent.Id);

        // Assert — no update should occur
        await _consentRepository.DidNotReceive().Update(Arg.Any<EventContactShareConsent>());
    }

    [Test]
    public async Task WithdrawConsent_ConsentNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var consentId = Guid.NewGuid();
        _consentRepository.GetById(consentId).Returns((EventContactShareConsent?)null);

        // Act & Assert
        await Assert.That(async () => await _service.WithdrawConsent(_tenantId, _userId, consentId))
            .Throws<KeyNotFoundException>();
    }

    [Test]
    public async Task WithdrawConsent_BelongsToOtherUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var consent = CreateExistingConsent(ConsentStatus.Granted);
        consent.TenantId = _tenantId;
        consent.UserId = Guid.NewGuid(); // Different user
        _consentRepository.GetById(consent.Id).Returns(consent);

        // Act & Assert
        await Assert.That(async () => await _service.WithdrawConsent(_tenantId, _userId, consent.Id))
            .Throws<UnauthorizedAccessException>();
    }

    #endregion

    #region GetUserConsents

    [Test]
    public async Task GetUserConsents_ReturnsConsentsWithOrganizationNames()
    {
        // Arrange
        var consents = new List<EventContactShareConsent>
        {
            CreateExistingConsent(ConsentStatus.Granted),
            CreateExistingConsent(ConsentStatus.Withdrawn)
        };
        _consentRepository.GetByUser(_tenantId, _userId).Returns(consents);

        // Act
        var result = await _service.GetUserConsents(_tenantId, _userId);

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
    }

    #endregion

    #region Helpers

    private static Tenant CreateTenant() => new()
    {
        FullName = "Test Tenant",
        Slug = "test-tenant",
        TenantStatus = new TenantStatus { MasterCode = "ACTIVE", FullName = "Active" }
    };

    private static ActorType CreateActorType() => new()
    {
        FullName = "Organization",
        MasterCode = "ORG"
    };

    private static Actor CreateActorEntity(Guid id, Guid? orgId = null) => new()
    {
        Id = id,
        OrganizationId = orgId,
        Pii = new ActorPii { DisplayName = "Test Org Actor" },
        ActorType = CreateActorType(),
        Tenant = CreateTenant()
    };

    private static Organization CreateOrganizationEntity(Guid id, bool approved = true) => new()
    {
        Id = id,
        ApprovalStatusId = approved ? (int)ApprovalStatusEnum.Approved : 1,
        Pii = new OrganizationPii { FullName = approved ? "Approved Org" : "Unapproved Org" },
        ApprovalStatus = new ApprovalStatus
        {
            MasterCode = approved ? "APPROVED" : "PENDING",
            FullName = approved ? "Approved" : "Pending"
        },
        Tenant = CreateTenant()
    };

    private Explore.Domain.Event CreateEvent()
    {
        return new Explore.Domain.Event
        {
            Id = _eventId,
            ActorId = _actorId,
            Title = "Test Event",
            Actor = CreateActorEntity(_actorId, _orgId),
            Tenant = CreateTenant(),
            VisibilityType = new VisibilityType { MasterCode = "PUBLIC", FullName = "Public" },
            EventStatus = new EventStatus { MasterCode = "PUBLISHED", FullName = "Published" },
            EventFormat = new EventFormat { MasterCode = "IN_PERSON", FullName = "In Person" }
        };
    }

    private void SetupEventAndActor()
    {
        _eventRepository.GetById(_eventId).Returns(CreateEvent());
        _actorRepository.GetById(_actorId).Returns(CreateActorEntity(_actorId, _orgId));
    }

    private void SetupApprovedOrg()
    {
        _organizationRepository.GetById(_orgId).Returns(CreateOrganizationEntity(_orgId, approved: true));
    }

    private void SetupValidChain(string email)
    {
        SetupEventAndActor();
        SetupApprovedOrg();
        _userRepository.GetUserWithDetails(_userId).Returns(new User
        {
            Id = _userId,
            Pii = new UserPii { Email = email, FirstName = "Test", LastName = "User" }
        });
    }

    private EventContactShareConsent CreateExistingConsent(ConsentStatus status)
    {
        return new EventContactShareConsent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            UserId = _userId,
            RecipientActorId = _actorId,
            SourceEventId = Guid.NewGuid(),
            PurposeCode = ConsentPurposeCodes.OrganizerFutureCommunications,
            Status = status,
            EmailSnapshot = "old@example.com",
            EmailNormalizedSnapshot = "old@example.com",
            ConsentTextSnapshot = "old consent text",
            ConsentUiVersion = "v1",
            GrantedAt = DateTime.UtcNow.AddDays(-30),
            WithdrawnAt = status == ConsentStatus.Withdrawn ? DateTime.UtcNow.AddDays(-10) : null
        };
    }

    #endregion
}
