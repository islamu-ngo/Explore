using Blazouter.Services;
using Explore.Blazor.Client.Pages.Events;
using Explore.Blazor.Client.Pages.Events.Models;
using MudBlazor;
using System.Reflection;

namespace Explore.Blazor.Client.Tests.Pages.Event;

/// <summary>
/// Component tests for CreateEvent page.
/// Tests form rendering, validation, and event creation flow.
/// </summary>
/// <remarks>
/// CreateEvent is a complex form with many external dependencies including
/// RouterStateService from Blazouter which cannot be easily mocked.
/// These tests focus on service integration verification.
/// </remarks>
public class CreateEventTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IEventService _eventService;
    private readonly IOrganizationService _organizationService;
    private readonly IUserService _userService;
    private readonly IAdminService _adminService;
    private readonly ICategoryService _categoryService;
    private readonly ITagService _tagService;
    private readonly ILocationService _locationService;
    private readonly IImageStorageService _imageStorageService;
    private readonly IEventTemplateService _eventTemplateService;
    private readonly IDialogService _dialogService;

    public CreateEventTests()
    {
        _ctx = new BlazorTestContext();
        _ctx.AddGroupServiceMock();

        // Create mocks for all required services
        _eventService = Substitute.For<IEventService>();
        _organizationService = Substitute.For<IOrganizationService>();
        _userService = Substitute.For<IUserService>();
        _adminService = Substitute.For<IAdminService>();
        _categoryService = Substitute.For<ICategoryService>();
        _tagService = Substitute.For<ITagService>();
        _locationService = Substitute.For<ILocationService>();
        _imageStorageService = Substitute.For<IImageStorageService>();
        _eventTemplateService = Substitute.For<IEventTemplateService>();
        _dialogService = Substitute.For<IDialogService>();

        // Register services
        _ctx.Services.AddSingleton(_eventService);
        _ctx.Services.AddSingleton(_organizationService);
        _ctx.Services.AddSingleton(_userService);
        _ctx.Services.AddSingleton(_adminService);
        _ctx.Services.AddSingleton(_categoryService);
        _ctx.Services.AddSingleton(_tagService);
        _ctx.Services.AddSingleton(_locationService);
        _ctx.Services.AddSingleton(_imageStorageService);
        _ctx.Services.AddSingleton(_eventTemplateService);

        // RouterStateService from Blazouter - register the real service
        _ctx.Services.AddScoped<RouterStateService>();

        _ctx.Services.AddSingleton(_dialogService);
        _ctx.Services.AddSingleton(Substitute.For<ISnackbar>());
        _ctx.Services.AddSingleton(Substitute.For<ILogger<CreateEvent>>());
        _ctx.Services.AddSingleton(MockServiceFactory.CreateNotificationService());
        _ctx.Services.AddSingleton(MockServiceFactory.CreateTranslationService());
        _ctx.Services.AddSingleton(Substitute.For<IHttpClientFactory>());

        var registrationPolicyMock = Substitute.For<IEventRegistrationPolicyService>();
        registrationPolicyMock.GetEventRegistrationPoliciesAsync()
            .Returns(new List<EventRegistrationPolicyListDto>());
        _ctx.Services.AddSingleton(registrationPolicyMock);

        // Setup default mock responses
        SetupDefaultMockResponses();
    }

    private void SetupDefaultMockResponses()
    {
        // User service
        var user = new UserDto { Id = Guid.NewGuid(), FirstName = "Test", LastName = "User", Email = "test@example.com" };
        _userService.GetCurrentUserAsync().Returns(user);

        // Organization service - user has permission to create events
        var orgId = Guid.NewGuid();
        var org = new OrganizationListDto
        {
            Id = orgId,
            FullName = "Test Organization",
            CurrentUserRole = 1 // Admin role
        };
        _organizationService.GetOrganizationsByUserAsync(Arg.Any<Guid>()).Returns(new List<OrganizationListDto> { org });
        _organizationService.GetMyOrganizationsAsync().Returns(new List<OrganizationListDto> { org });

        // Admin service lookups
        _adminService.GetEventTypesAsync().Returns(new List<EventTypeListDto>
        {
            new() { Id = 1, FullName = "Conference" },
            new() { Id = 2, FullName = "Workshop" }
        });
        _adminService.GetAudienceGendersAsync().Returns(new List<AudienceGenderListDto>
        {
            new() { Id = 1, FullName = "Mixed" },
            new() { Id = 2, FullName = "Men Only" }
        });
        _adminService.GetAudienceAgesAsync().Returns(new List<AudienceAgeListDto>
        {
            new() { Id = 1, FullName = "All Ages" },
            new() { Id = 2, FullName = "Adults" }
        });
        _adminService.GetEventFormatsAsync().Returns(new List<EventFormatListDto>
        {
            new() { Id = 1, FullName = "In-Person" },
            new() { Id = 2, FullName = "Online" }
        });
        _adminService.GetVisibilityTypesAsync().Returns(new List<VisibilityTypeListDto>
        {
            new() { Id = 1, FullName = "Public" },
            new() { Id = 2, FullName = "Private" }
        });
        _adminService.GetMadhabsAsync().Returns(new List<MadhabListDto>());
        _adminService.GetRegistrationModesAsync().Returns(new List<RegistrationModeListDto>
        {
            new() { Id = 1, FullName = "Open" },
            new() { Id = 2, FullName = "Approval Required" }
        });
        _adminService.GetLanguagesAsync().Returns(new List<LanguageListDto>
        {
            new() { Id = 1, FullName = "English" },
            new() { Id = 2, FullName = "Arabic" }
        });

        // Category and Tag services
        _categoryService.GetAllCategoriesAsync().Returns(new List<CategoryListDto>());
        _tagService.GetAllTagsAsync().Returns(new List<TagListDto>());

        // Location service
        _locationService.GetAllLocationsAsync().Returns(new List<LocationListDto>());

        // Event service defaults
        var createdEventId = Guid.NewGuid();
        var concurrencyStamp = Guid.NewGuid();
        _eventService.CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>()).Returns(new BaseCommandResponseOfGuid
        {
            Success = true,
            Id = createdEventId
        });
        _eventService.GetEventPublishReadinessAsync(createdEventId, Arg.Any<CancellationToken>()).Returns(new EventPublishReadinessDto
        {
            EventId = createdEventId,
            IsReady = true,
            Errors = new List<EventPublishReadinessErrorDto>()
        });
        _eventService.GetEventByIdAsync(createdEventId).Returns(new EventDto
        {
            Id = createdEventId,
            Title = "Template Event",
            ConcurrencyStamp = concurrencyStamp
        });
        _eventService.PublishEventAsync(createdEventId, concurrencyStamp, Arg.Any<CancellationToken>()).Returns(new BaseCommandResponseOfGuid
        {
            Success = true,
            Id = createdEventId
        });
        _dialogService.ShowMessageBoxAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DialogOptions>())
            .Returns(true);
        _eventService.GetEventCreationContextAsync(Arg.Any<CancellationToken>()).Returns(new EventCreationContextDto
        {
            CanCreate = true,
            AllowPersonalPublishing = true,
            AllowOrganizationPublishing = true,
            AllowGroupPublishing = true,
            DefaultPublisherMode = "personal",
            PublisherOptions = new List<EventCreationPublisherOptionDto>
            {
                new()
                {
                    PublisherMode = "personal",
                    DisplayName = "Personal profile",
                    CanPublish = true
                },
                new()
                {
                    PublisherMode = "organization",
                    PublisherId = orgId,
                    DisplayName = "Test Organization",
                    RoleId = 1,
                    CanPublish = true
                }
            }
        });
        _eventService.GetSessionsByEventAsync(Arg.Any<Guid>()).Returns(new List<EventSessionListDto>());

        // Template service defaults
        _eventTemplateService.GetTemplatesAsync(
                Arg.Any<int?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(PaginatedResult<EventTemplateListModel>.Empty(pageSize: 100));
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    #region Rendering Tests

    [Test]
    public async Task CreateEvent_RendersWithoutCrash()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        // Act - Component should render without throwing
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Length > 0, TimeSpan.FromSeconds(2));

        // Assert - Component rendered and produced markup
        await Assert.That(cut.Markup).IsNotEmpty();
    }

    [Test]
    public async Task CreateEvent_RendersDraftShellWithoutSeedSessionFields()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        // Act
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).DoesNotContain("Initial session timing");
        await Assert.That(cut.Markup).DoesNotContain("Session date");
        await Assert.That(cut.Markup).DoesNotContain("Session location");
    }

    [Test]
    public async Task CreateEvent_ShowsErrorOrForm_WhenRendered()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        // Act
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(
            () => cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase)
               || cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        // Assert - Should render either the form or an error state
        // Without valid organizationId from RouterState, it shows permission error
        var hasForm = cut.Markup.Contains("mud-input");
        var hasError = cut.Markup.Contains("mud-alert");
        await Assert.That(hasForm || hasError).IsTrue();
    }

    #endregion

    #region Behavior Interaction Tests

    [Test]
    public async Task CreateEvent_WhenRendered_RequestsLookupDataFromAdminService()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        // Act
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        // Assert - component behavior triggers lookup requests
        await _adminService.Received(1).GetEventTypesAsync();
        await _adminService.Received(1).GetAudienceGendersAsync();
        await _adminService.Received(1).GetAudienceAgesAsync();
        await _adminService.Received(1).GetEventFormatsAsync();
        await _adminService.Received(1).GetVisibilityTypesAsync();
        await _adminService.Received(1).GetRegistrationModesAsync();
        await _adminService.Received(1).GetLanguagesAsync();
    }

    [Test]
    public async Task CreateEvent_WhenRendered_LoadsTemplateListForCurrentEventType()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        // Act
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        // Assert
        await _eventTemplateService.Received(1).GetTemplatesAsync(null, 1, 100, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateEvent_WhenEventTypeChanges_ClearsTemplateSelectionAndReloadsScopedTemplates()
    {
        // Arrange
        var selectedTemplateId = Guid.NewGuid();
        var scopedTemplate = CreateTemplateListModel(Guid.NewGuid(), "Workshop Template", eventTypeId: 2);
        _eventTemplateService.GetTemplateByIdAsync(selectedTemplateId, Arg.Any<CancellationToken>())
            .Returns(CreateTemplateDetailModel(selectedTemplateId, "Selected Template", eventTypeId: 1));
        _eventTemplateService.GetTemplatesAsync(2, 1, 100, Arg.Any<CancellationToken>())
            .Returns(CreateTemplatePage(scopedTemplate));

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        await InvokePrivateAsync(cut.Instance, "OnEventTemplateChanged", selectedTemplateId);

        // Act
        await InvokePrivateAsync(cut.Instance, "OnEventTypeChanged", 2);

        // Assert
        var dto = GetPrivateField<CreateEventDraftRequestDto>(cut.Instance, "createDto");
        var templates = GetPrivateField<IReadOnlyList<EventTemplateListModel>>(cut.Instance, "eventTemplates");
        var selectedDetail = GetPrivateField<EventTemplateDetailModel?>(cut.Instance, "_selectedEventTemplate");

        await Assert.That(dto.EventTypeId).IsEqualTo(2);
        await Assert.That(dto.TemplateId).IsNull();
        await Assert.That(selectedDetail).IsNull();
        await Assert.That(templates.Count).IsEqualTo(1);
        await Assert.That(templates[0].Id).IsEqualTo(scopedTemplate.Id);
        await _eventTemplateService.Received(1).GetTemplatesAsync(2, 1, 100, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateEvent_WhenTemplatePreviewFails_ClearsTemplateIdForVanillaSubmit()
    {
        // Arrange
        var missingTemplateId = Guid.NewGuid();
        _eventTemplateService.GetTemplateByIdAsync(missingTemplateId, Arg.Any<CancellationToken>())
            .Returns((EventTemplateDetailModel?)null);

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        // Act
        await InvokePrivateAsync(cut.Instance, "OnEventTemplateChanged", missingTemplateId);

        // Assert
        var dto = GetPrivateField<CreateEventDraftRequestDto>(cut.Instance, "createDto");
        var error = GetPrivateField<string?>(cut.Instance, "_templateLoadError");

        await Assert.That(dto.TemplateId).IsNull();
        await Assert.That(error).Contains("selection was cleared");
    }

    [Test]
    public async Task CreateEvent_WhenTemplatePreviewRequestsRace_KeepsLatestSelectionOnly()
    {
        // Arrange
        var slowTemplateId = Guid.NewGuid();
        var fastTemplateId = Guid.NewGuid();
        var slowPreview = new TaskCompletionSource<EventTemplateDetailModel?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var fastPreview = CreateTemplateDetailModel(fastTemplateId, "Fast Template", eventTypeId: 1);

        _eventTemplateService.GetTemplateByIdAsync(slowTemplateId, Arg.Any<CancellationToken>())
            .Returns(slowPreview.Task);
        _eventTemplateService.GetTemplateByIdAsync(fastTemplateId, Arg.Any<CancellationToken>())
            .Returns(fastPreview);

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        // Act
        var slowRequest = InvokePrivateAsync(cut.Instance, "OnEventTemplateChanged", slowTemplateId);
        await InvokePrivateAsync(cut.Instance, "OnEventTemplateChanged", fastTemplateId);
        slowPreview.SetResult(CreateTemplateDetailModel(slowTemplateId, "Slow Template", eventTypeId: 1));
        await slowRequest;

        // Assert
        var dto = GetPrivateField<CreateEventDraftRequestDto>(cut.Instance, "createDto");
        var selectedDetail = GetPrivateField<EventTemplateDetailModel?>(cut.Instance, "_selectedEventTemplate");

        await Assert.That(dto.TemplateId).IsEqualTo(fastTemplateId);
        await Assert.That(selectedDetail?.Id).IsEqualTo(fastTemplateId);
    }

    [Test]
    public async Task CreateEvent_WhenTemplateListRequestsRace_KeepsLatestEventTypeTemplatesOnly()
    {
        // Arrange
        var slowTemplate = CreateTemplateListModel(Guid.NewGuid(), "Conference Template", eventTypeId: 1);
        var fastTemplate = CreateTemplateListModel(Guid.NewGuid(), "Workshop Template", eventTypeId: 2);
        var slowTemplates = new TaskCompletionSource<PaginatedResult<EventTemplateListModel>>(TaskCreationOptions.RunContinuationsAsynchronously);

        _eventTemplateService.GetTemplatesAsync(1, 1, 100, Arg.Any<CancellationToken>())
            .Returns(slowTemplates.Task);
        _eventTemplateService.GetTemplatesAsync(2, 1, 100, Arg.Any<CancellationToken>())
            .Returns(CreateTemplatePage(fastTemplate));

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        // Act
        var slowRequest = InvokePrivateAsync(cut.Instance, "OnEventTypeChanged", 1);
        await InvokePrivateAsync(cut.Instance, "OnEventTypeChanged", 2);
        slowTemplates.SetResult(CreateTemplatePage(slowTemplate));
        await slowRequest;

        // Assert
        var dto = GetPrivateField<CreateEventDraftRequestDto>(cut.Instance, "createDto");
        var templates = GetPrivateField<IReadOnlyList<EventTemplateListModel>>(cut.Instance, "eventTemplates");

        await Assert.That(dto.EventTypeId).IsEqualTo(2);
        await Assert.That(templates.Count).IsEqualTo(1);
        await Assert.That(templates[0].Id).IsEqualTo(fastTemplate.Id);
    }

    [Test]
    public async Task CreateEvent_WhenTemplatePreviewIsLoading_DoesNotSubmitTemplateId()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var preview = new TaskCompletionSource<EventTemplateDetailModel?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _eventTemplateService.GetTemplateByIdAsync(templateId, Arg.Any<CancellationToken>())
            .Returns(preview.Task);

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        var previewRequest = InvokePrivateAsync(cut.Instance, "OnEventTemplateChanged", templateId);
        PrepareValidSubmitState(cut.Instance);

        // Act
        await InvokePrivateAsync(cut.Instance, "HandleSubmit");

        // Assert
        await _eventService.DidNotReceive().CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>());
        var error = GetPrivateField<string>(cut.Instance, "errorMessage");
        await Assert.That(error).Contains("template preview");

        preview.SetResult(null);
        await previewRequest;
    }

    [Test]
    public async Task CreateEvent_WhenSubmitted_UsesSelectedTemplateId()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        _eventTemplateService.GetTemplateByIdAsync(templateId, Arg.Any<CancellationToken>())
            .Returns(CreateTemplateDetailModel(templateId, "Selected Template", eventTypeId: 1));

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        await InvokePrivateAsync(cut.Instance, "OnEventTemplateChanged", templateId);
        PrepareValidSubmitState(cut.Instance);

        // Act
        await InvokePrivateAsync(cut.Instance, "HandleSubmit");

        // Assert
        await _eventService.Received(1).CreateEventAsync(Arg.Is<CreateEventDraftRequestDto>(dto => dto != null && dto.TemplateId == templateId));
    }

    [Test]
    public async Task CreateEvent_WhenSubmittedWithoutTemplate_SendsNullTemplateId()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        PrepareValidSubmitState(cut.Instance);

        // Act
        await InvokePrivateAsync(cut.Instance, "HandleSubmit");

        // Assert
        await _eventService.Received(1).CreateEventAsync(Arg.Is<CreateEventDraftRequestDto>(dto => dto != null && dto.TemplateId == null));
    }

    [Test]
    public async Task CreateEvent_WhenParentTemplateChanges_ClearsExistingSessionTemplateIdsBeforeSubmit()
    {
        // Arrange
        var staleSessionTemplateId = Guid.NewGuid();
        var newParentTemplateId = Guid.NewGuid();
        _eventTemplateService.GetTemplateByIdAsync(newParentTemplateId, Arg.Any<CancellationToken>())
            .Returns(CreateTemplateDetailModel(newParentTemplateId, "New Parent Template", eventTypeId: 1));
        _eventService.CreateSessionAsync(Arg.Any<CreateEventSessionDto>()).Returns(new BaseCommandResponseOfGuid
        {
            Success = true,
            Id = Guid.NewGuid()
        });

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        PrepareValidSubmitState(cut.Instance);
        var sessions = GetPrivateField<List<SessionEditorModel>>(cut.Instance, "sessions");
        sessions.Add(new SessionEditorModel
        {
            Title = "Breakout Session",
            StartTime = DateTime.Today.AddHours(11),
            EndTime = DateTime.Today.AddHours(12),
            RegistrationModeId = 1,
            SessionTemplateId = staleSessionTemplateId
        });

        // Act
        await InvokePrivateAsync(cut.Instance, "OnEventTemplateChanged", newParentTemplateId);
        await InvokePrivateAsync(cut.Instance, "HandleSubmit");

        // Assert
        await Assert.That(sessions.All(session => session.SessionTemplateId is null)).IsTrue();
        await _eventService.Received(1).CreateEventAsync(
            Arg.Is<CreateEventDraftRequestDto>(request => request.TemplateId == newParentTemplateId));
    }

    [Test]
    public async Task CreateEvent_WhenRendered_LoadsUserAndOrganizationContext()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        // Act
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        // Assert - component behavior loads principal context dependencies
        await _userService.Received(1).GetCurrentUserAsync();
        await _eventService.Received(1).GetEventCreationContextAsync(Arg.Any<CancellationToken>());
        await _organizationService.Received().GetMyOrganizationsAsync();
        await _categoryService.Received().GetAllCategoriesAsync();
        await _tagService.Received().GetAllTagsAsync();
        await _locationService.Received().GetAllLocationsAsync();
    }

    [Test]
    public async Task CreateEvent_WhenCreationContextBlocksCreate_ShowsUnavailableReasonAndDoesNotSubmit()
    {
        // Arrange
        _eventService.GetEventCreationContextAsync(Arg.Any<CancellationToken>()).Returns(new EventCreationContextDto
        {
            CanCreate = false,
            AllowPersonalPublishing = false,
            AllowOrganizationPublishing = false,
            AllowGroupPublishing = false,
            UnavailableReason = "No available publisher can create events for the current user.",
            PublisherOptions = new List<EventCreationPublisherOptionDto>()
        });
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("No available publisher can create events", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Creation context unavailable reason was not rendered.");
            }
        }, TimeSpan.FromSeconds(3));

        PrepareValidSubmitState(cut.Instance);

        // Act
        await InvokePrivateAsync(cut.Instance, "HandleSubmit");

        // Assert
        await _eventService.DidNotReceive().CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>());
        var error = GetPrivateField<string>(cut.Instance, "errorMessage");
        await Assert.That(error).Contains("No available publisher");
    }

    [Test]
    public async Task CreateEvent_WhenReviewPublishFindsReadinessErrors_SavesDraftAndShowsErrors()
    {
        // Arrange
        var createdEventId = Guid.NewGuid();
        _eventService.CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>()).Returns(new BaseCommandResponseOfGuid
        {
            Success = true,
            Id = createdEventId
        });
        _eventService.GetEventPublishReadinessAsync(createdEventId, Arg.Any<CancellationToken>()).Returns(new EventPublishReadinessDto
        {
            EventId = createdEventId,
            IsReady = false,
            Errors = new List<EventPublishReadinessErrorDto>
            {
                new()
                {
                    Code = "schedule_session_required",
                    FieldPath = "schedule.sessions",
                    Message = "At least one scheduled session is required before publishing.",
                    Severity = "error"
                }
            }
        });
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));
        PrepareValidSubmitState(cut.Instance);

        // Act
        await InvokePrivateAsync(cut.Instance, "HandleSubmit");

        // Assert
        await _eventService.Received(1).CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>());
        await _eventService.Received(1).GetEventPublishReadinessAsync(createdEventId, Arg.Any<CancellationToken>());
        await _eventService.DidNotReceive().PublishEventAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        var error = GetPrivateField<string>(cut.Instance, "errorMessage");
        var readinessErrors = GetPrivateField<IReadOnlyList<EventPublishReadinessErrorDto>>(cut.Instance, "_publishReadinessErrors");
        await Assert.That(error).Contains("not ready to publish");
        await Assert.That(readinessErrors.Count).IsEqualTo(1);
        await Assert.That(readinessErrors[0].FieldPath).IsEqualTo("schedule.sessions");
        await Assert.That(GetPrivateField<bool>(cut.Instance, "_isMoreOptionsExpanded")).IsTrue();
    }

    [Test]
    public async Task AddSessionAsync_SavesDraftAndNavigatesToDedicatedSessionComposer()
    {
        // Arrange
        var createdEventId = Guid.NewGuid();
        _eventService.CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>()).Returns(new BaseCommandResponseOfGuid
        {
            Success = true,
            Id = createdEventId
        });
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));
        PrepareValidSubmitState(cut.Instance);

        // Act
        await InvokePrivateAsync(cut.Instance, "AddSessionAsync");

        // Assert
        await _eventService.Received(1).CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>());
        await _eventService.DidNotReceive().GetEventPublishReadinessAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        var navigation = _ctx.Services.GetRequiredService<NavigationManager>();
        await Assert.That(navigation.Uri).EndsWith($"/events/{createdEventId}/sessions/create");
    }

    [Test]
    public async Task CreateEvent_WhenReviewPublishIsReady_PublishesDraftWithConcurrencyStamp()
    {
        // Arrange
        var createdEventId = Guid.NewGuid();
        var concurrencyStamp = Guid.NewGuid();
        _eventService.CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>()).Returns(new BaseCommandResponseOfGuid
        {
            Success = true,
            Id = createdEventId
        });
        _eventService.GetEventPublishReadinessAsync(createdEventId, Arg.Any<CancellationToken>()).Returns(new EventPublishReadinessDto
        {
            EventId = createdEventId,
            IsReady = true,
            Errors = new List<EventPublishReadinessErrorDto>()
        });
        _eventService.GetEventByIdAsync(createdEventId).Returns(new EventDto
        {
            Id = createdEventId,
            Title = "Template Event",
            ConcurrencyStamp = concurrencyStamp
        });
        _eventService.PublishEventAsync(createdEventId, concurrencyStamp, Arg.Any<CancellationToken>()).Returns(new BaseCommandResponseOfGuid
        {
            Success = true,
            Id = createdEventId
        });
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));
        PrepareValidSubmitState(cut.Instance);

        // Act
        await InvokePrivateAsync(cut.Instance, "HandleSubmit");

        // Assert
        await _eventService.Received(1).CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>());
        await _eventService.Received(1).GetEventPublishReadinessAsync(createdEventId, Arg.Any<CancellationToken>());
        await _eventService.Received(1).GetEventByIdAsync(createdEventId);
        await _dialogService.Received(1).ShowMessageBoxAsync(
            "Review and publish",
            Arg.Is<string>(message => message.Contains("Publish 'Template Event' now?", StringComparison.OrdinalIgnoreCase)
                && message.Contains("Program:", StringComparison.OrdinalIgnoreCase)
                && message.Contains("Timezone:", StringComparison.OrdinalIgnoreCase)),
            "Publish event",
            Arg.Any<string>(),
            "Keep draft",
            Arg.Any<DialogOptions>());
        await _eventService.Received(1).PublishEventAsync(createdEventId, concurrencyStamp, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateEvent_WhenReviewPublishConfirmationIsCanceled_KeepsDraftWithoutPublishing()
    {
        // Arrange
        var createdEventId = Guid.NewGuid();
        var concurrencyStamp = Guid.NewGuid();
        _eventService.CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>()).Returns(new BaseCommandResponseOfGuid
        {
            Success = true,
            Id = createdEventId
        });
        _eventService.GetEventPublishReadinessAsync(createdEventId, Arg.Any<CancellationToken>()).Returns(new EventPublishReadinessDto
        {
            EventId = createdEventId,
            IsReady = true,
            Errors = new List<EventPublishReadinessErrorDto>()
        });
        _eventService.GetEventByIdAsync(createdEventId).Returns(new EventDto
        {
            Id = createdEventId,
            Title = "Template Event",
            ConcurrencyStamp = concurrencyStamp
        });
        _dialogService.ShowMessageBoxAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DialogOptions>())
            .Returns(false);
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));
        PrepareValidSubmitState(cut.Instance);

        // Act
        await InvokePrivateAsync(cut.Instance, "HandleSubmit");

        // Assert
        await _eventService.Received(1).CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>());
        await _eventService.Received(1).GetEventPublishReadinessAsync(createdEventId, Arg.Any<CancellationToken>());
        await _eventService.Received(1).GetEventByIdAsync(createdEventId);
        await _eventService.DidNotReceive().PublishEventAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        var error = GetPrivateField<string>(cut.Instance, "errorMessage");
        await Assert.That(error).Contains("draft was saved");
    }

    [Test]
    public async Task CreateEvent_RendersPublicationContextSelector()
    {
        // Arrange
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        // Act
        var cut = _ctx.RenderMudComponent<CreateEvent>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Publishing as", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Publication context selector was not rendered.");
            }

            if (cut.Markup.Contains("create-event__publisher-btn", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Legacy publisher mode buttons were rendered.");
            }
        }, TimeSpan.FromSeconds(3));

        await _eventService.Received(1).GetEventCreationContextAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateEvent_ShowsReviewAction()
    {
        // The form is now a single-page layout (no multi-step wizard).
        // The primary publish-intent action should be visible immediately.
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();

        cut.WaitForAssertion(() =>
        {
            var buttons = cut.FindAll("button");
            var hasReviewButton = buttons.Any(b => b.TextContent.Contains("Review and publish", StringComparison.OrdinalIgnoreCase));
            if (!hasReviewButton)
            {
                throw new InvalidOperationException("Review action was not rendered.");
            }
        }, TimeSpan.FromSeconds(3));
    }

    [Test]
    public async Task CreateEvent_RendersProgressiveDisclosureSections()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Program summary", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Program items", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Event settings", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Template and custom fields", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Progressive disclosure sections were not rendered.");
            }

            if (cut.Markup.Contains("Open schedule timeline", StringComparison.OrdinalIgnoreCase)
                || cut.Markup.Contains("Add sessions, day labels, rooms, or agenda", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Legacy schedule toggle copy was rendered.");
            }
        }, TimeSpan.FromSeconds(3));
    }

    [Test]
    public async Task CreateEvent_RendersThemeQuickBarUnderImageBeforeSettings()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();

        cut.WaitForAssertion(() =>
        {
            var eventImageIndex = cut.Markup.IndexOf("Event Image", StringComparison.OrdinalIgnoreCase);
            var moreOptionsIndex = cut.Markup.IndexOf("More options", StringComparison.OrdinalIgnoreCase);
            var eventSettingsIndex = cut.Markup.IndexOf("Event settings", StringComparison.OrdinalIgnoreCase);
            var themeIndex = cut.Markup.IndexOf("Theme", StringComparison.OrdinalIgnoreCase);
            var advancedThemeIndex = cut.Markup.IndexOf("Advanced theme options", StringComparison.OrdinalIgnoreCase);

            if (eventImageIndex < 0 || eventSettingsIndex < 0 || moreOptionsIndex < 0 || themeIndex < 0 || advancedThemeIndex < 0)
            {
                throw new InvalidOperationException("Expected event image, theme quick controls, event settings, and more options to render.");
            }

            if (themeIndex < eventImageIndex || themeIndex > eventSettingsIndex || themeIndex > moreOptionsIndex)
            {
                throw new InvalidOperationException("Theme quick controls should render under the image before Event settings and More options.");
            }

            if (cut.Markup.Contains("Event Appearance", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Rejected Event Appearance accordion copy was rendered.");
            }

            if (!cut.Markup.Contains("Visibility, audience, registration, classification, categories, and tags.", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Template and custom fields.", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Revised Event settings and narrow More options copy was not rendered.");
            }
        }, TimeSpan.FromSeconds(3));
    }

    [Test]
    public async Task CreateEvent_OpensThemeStudioTrayFromQuickBar()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Advanced theme options", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Theme quick bar advanced action was not rendered.");
            }
        }, TimeSpan.FromSeconds(3));

        var advancedButton = cut.FindAll("button")
            .First(button => button.TextContent.Contains("Advanced theme options", StringComparison.OrdinalIgnoreCase));
        advancedButton.Click();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Theme studio", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Advanced styling controls", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Use this tray for precise styling", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Theme studio tray was not rendered after opening from the quick bar.");
            }

            if (cut.Markup.Contains("Presets gallery", StringComparison.OrdinalIgnoreCase)
                || cut.Markup.Contains("Quick advanced controls", StringComparison.OrdinalIgnoreCase)
                || cut.Markup.Contains("Background Image URL", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Create Event must not duplicate quick controls or render unsupported raw background image URL controls in the tray.");
            }
        }, TimeSpan.FromSeconds(3));
    }

    [Test]
    public async Task CreateEvent_RendersProgramSummaryBeforeEventSettings()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Program summary", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Program items", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Talks, workshops, panels, classes, and activities are saved as sessions", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Breaks, rooms, and day details stay in logistics", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Add session", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Add section/track", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Program Summary section was not rendered with the expected session-first guidance.");
            }

            if (cut.Markup.Contains("child event", StringComparison.OrdinalIgnoreCase)
                || cut.Markup.Contains("subevent", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Program Summary must not reintroduce child-event language.");
            }
        }, TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup.IndexOf("Program summary", StringComparison.OrdinalIgnoreCase))
            .IsLessThan(cut.Markup.IndexOf("Event settings", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task CreateEvent_DoesNotRenderScheduleLogisticsComposerInEventShell()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Program summary", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Add session", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Program summary was not rendered after removing the schedule composer.");
            }

            var forbiddenCopies = new[]
            {
                "Schedule timeline composer",
                "Open schedule timeline",
                "Add to this day",
                "Add another session",
                "Scheduling (Days, Rooms & Agenda)",
                "Multiple sessions",
                "Day labels",
                "Room setup",
                "Agenda builder",
                "create-event__session-drawer",
                "Edit previous session",
                "Edit next session",
                "Add event location",
                "Event languages"
            };

            if (forbiddenCopies.Any(copy => cut.Markup.Contains(copy, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Legacy schedule logistics UI was rendered in the event shell.");
            }
        }, TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup.Contains("Schedule timeline composer", StringComparison.OrdinalIgnoreCase)).IsFalse();
    }

    #endregion

    private static PaginatedResult<EventTemplateListModel> CreateTemplatePage(params EventTemplateListModel[] templates) => new()
    {
        Items = templates.ToList(),
        PageNumber = 1,
        PageSize = 100,
        TotalCount = templates.Length
    };

    private static EventTemplateListModel CreateTemplateListModel(Guid id, string displayName, int eventTypeId) => new()
    {
        Id = id,
        TemplateKey = displayName.ToLowerInvariant().Replace(' ', '-'),
        DisplayName = displayName,
        EventTypeId = eventTypeId,
        Version = 1,
        IsActive = true,
        IsPublished = true,
        DefinitionsCount = 1
    };

    private static EventTemplateDetailModel CreateTemplateDetailModel(Guid id, string displayName, int eventTypeId) => new()
    {
        Id = id,
        TemplateKey = displayName.ToLowerInvariant().Replace(' ', '-'),
        DisplayName = displayName,
        EventTypeId = eventTypeId,
        Version = 1,
        IsActive = true,
        IsPublished = true,
        Definitions = new List<EventTemplateDefinitionModel>
        {
            new()
            {
                Key = "topic",
                DisplayName = "Topic",
                SortOrder = 1
            }
        }
    };

    private static async Task InvokePrivateAsync(CreateEvent component, string methodName, params object?[] args)
    {
        var method = typeof(CreateEvent).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(CreateEvent).FullName, methodName);

        var result = method.Invoke(component, args);
        if (result is Task task)
        {
            await task;
            return;
        }

        throw new InvalidOperationException($"Private method {methodName} did not return a Task.");
    }

    private static T GetPrivateField<T>(CreateEvent component, string fieldName)
    {
        var field = typeof(CreateEvent).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(CreateEvent).FullName, fieldName);

        return (T)field.GetValue(component)!;
    }

    private static void SetPrivateField<T>(CreateEvent component, string fieldName, T value)
    {
        var field = typeof(CreateEvent).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(CreateEvent).FullName, fieldName);

        field.SetValue(component, value);
    }

    private static void PrepareValidSubmitState(CreateEvent component)
    {
        var dto = GetPrivateField<CreateEventDraftRequestDto>(component, "createDto");
        dto.Title = "Template Event";
        dto.EventTypeId = 1;
        dto.EventFormatId = 1;
        dto.VisibilityTypeId = 1;
        dto.VisibilityTypeId = 2;

        SetPrivateField(component, "sessions", new List<SessionEditorModel>());
    }
}
