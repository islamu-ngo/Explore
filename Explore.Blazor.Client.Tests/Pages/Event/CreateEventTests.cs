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

        _ctx.Services.AddSingleton(Substitute.For<IDialogService>());
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
        _eventService.CreateEventAsync(Arg.Any<CreateEventDto>()).Returns(new BaseCommandResponseOfGuid
        {
            Success = true,
            Id = Guid.NewGuid()
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
        var dto = GetPrivateField<CreateEventDto>(cut.Instance, "createDto");
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
        var dto = GetPrivateField<CreateEventDto>(cut.Instance, "createDto");
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
        var dto = GetPrivateField<CreateEventDto>(cut.Instance, "createDto");
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
        var dto = GetPrivateField<CreateEventDto>(cut.Instance, "createDto");
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
        await _eventService.DidNotReceive().CreateEventAsync(Arg.Any<CreateEventDto>());
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
        await _eventService.Received(1).CreateEventAsync(Arg.Is<CreateEventDto>(dto => dto != null && dto.TemplateId == templateId));
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
        await _eventService.Received(1).CreateEventAsync(Arg.Is<CreateEventDto>(dto => dto != null && dto.TemplateId == null));
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
        await _eventService.Received(1).CreateSessionAsync(
            Arg.Is<CreateEventSessionDto>(dto => dto != null && dto.SessionTemplateId == null));
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
        await _organizationService.Received().GetMyOrganizationsAsync();
        await _categoryService.Received().GetAllCategoriesAsync();
        await _tagService.Received().GetAllTagsAsync();
        await _locationService.Received().GetAllLocationsAsync();
    }

    [Test]
    public async Task CreateEvent_ShowsCreateButton()
    {
        // The form is now a single-page layout (no multi-step wizard).
        // The "Create Event" submit button should be visible immediately.
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();

        cut.WaitForAssertion(() =>
        {
            var buttons = cut.FindAll("button");
            var hasCreateButton = buttons.Any(b => b.TextContent.Contains("Create Event", StringComparison.OrdinalIgnoreCase));
            if (!hasCreateButton)
            {
                throw new InvalidOperationException("Create Event button was not rendered.");
            }
        }, TimeSpan.FromSeconds(3));
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
        var dto = GetPrivateField<CreateEventDto>(component, "createDto");
        dto.Title = "Template Event";
        dto.EventTypeId = 1;
        dto.EventFormatId = 1;
        dto.VisibilityTypeId = 1;
        dto.EventStatusId = 2;

        SetPrivateField(component, "sessions", new List<SessionEditorModel>
        {
            new()
            {
                Title = "Opening Session",
                StartTime = DateTime.Today.AddHours(9),
                EndTime = DateTime.Today.AddHours(10),
                RegistrationModeId = 1
            }
        });
    }
}
