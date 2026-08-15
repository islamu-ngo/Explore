// ABOUTME: Component tests for CreateEvent draft creation, upload binding, and publish handoff behavior.
// ABOUTME: Verifies single-page create flow preserves user-entered data before navigating to program setup.

using System.Reflection;
using Blazouter.Services;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Pages.Events;
using Explore.Blazor.Client.Pages.Events.Models;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

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
        // Create mocks for all required services
        _eventService = Substitute.For<IEventService>();
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
        _ctx.Services.AddScoped<MainContentAppearanceState>();

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

        var orgId = Guid.NewGuid();
        var org = new OrganizationListDto
        {
            Id = orgId,
            FullName = "Test Organization"
        };

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
        _eventService.GetSessionsByEventAsync(Arg.Any<Guid>(), Arg.Any<bool>()).Returns(new List<EventSessionListDto>());

        // Template service defaults
        _eventTemplateService.GetTemplatesAsync(
                Arg.Any<int?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new HalCollectionResourceOfEventTemplateListDto
            {
                PageNumber = 1,
                PageSize = 100,
                TotalCount = 0,
                TotalPages = 0,
                _embedded = new HalCollectionEmbeddedOfEventTemplateListDto { Items = [] }
            });
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    #region Rendering Tests

    [Test]
    public async Task CreateEvent_DefaultsToExplicitInformationOnlyParticipation()
    {
        var component = new CreateEvent();
        var configuration = GetPrivateField<CreateEventDraftRequestDto>(component, "createDto").ParticipationConfiguration;

        await Assert.That(configuration).IsNotNull();
        await Assert.That(configuration.ParticipationHandlingModeId).IsEqualTo(1);
        await Assert.That(configuration.AdvanceRegistrationObligationId).IsEqualTo(1);
        await Assert.That(configuration.IdentityAccessModeId).IsNull();
        await Assert.That(configuration.GuestRecoveryPolicy).IsNull();
    }

    [Test]
    public async Task CreateEvent_RendersExplicitParticipationControlsWithDefaultState()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForElement("[data-testid='participation-handling-select']");

        await Assert.That(ParticipationSelect(cut, "Participation handling").Instance.Value).IsEqualTo(1);
        await Assert.That(ParticipationSelect(cut, "Advance registration").Instance.Value).IsEqualTo(1);
        await Assert.That(ParticipationSelect(cut, "Advance registration").Instance.Disabled).IsTrue();
        await Assert.That(cut.FindComponents<MudSelect<int?>>().Any(item => item.Instance.Label == "Identity access")).IsFalse();
        await Assert.That(cut.FindComponents<MudSelect<int?>>().Any(item => item.Instance.Label == "Guest recovery")).IsFalse();
    }

    [Test]
    public async Task SaveDraft_WithExplicitExternalParticipation_UsesSelectedTypedValues()
    {
        CreateEventDraftRequestDto? capturedRequest = null;
        _eventService.CreateEventAsync(Arg.Do<CreateEventDraftRequestDto>(dto => capturedRequest = dto))
            .Returns(new BaseCommandResponseOfGuid { Success = true, Id = Guid.NewGuid() });
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForElement("[data-testid='participation-handling-select']");
        PrepareValidSubmitState(cut.Instance);

        await cut.InvokeAsync(() => ParticipationSelect(cut, "Participation handling").Instance.ValueChanged.InvokeAsync(3));
        await cut.InvokeAsync(() => ParticipationSelect(cut, "Advance registration").Instance.ValueChanged.InvokeAsync(3));

        await InvokePrivateAsync(cut.Instance, "SaveAsDraftAsync");

        await Assert.That(capturedRequest).IsNotNull();
        var configuration = capturedRequest!.ParticipationConfiguration;
        await Assert.That(configuration.ParticipationHandlingModeId).IsEqualTo(3);
        await Assert.That(configuration.AdvanceRegistrationObligationId).IsEqualTo(3);
        await Assert.That(configuration.IdentityAccessModeId).IsNull();
        await Assert.That(configuration.GuestRecoveryPolicy).IsNull();
    }

    [Test]
    public async Task SaveDraft_WithPlatformAccountParticipation_UsesSelectedTypedValues()
    {
        CreateEventDraftRequestDto? capturedRequest = null;
        _eventService.CreateEventAsync(Arg.Do<CreateEventDraftRequestDto>(dto => capturedRequest = dto))
            .Returns(new BaseCommandResponseOfGuid { Success = true, Id = Guid.NewGuid() });
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForElement("[data-testid='participation-handling-select']");
        PrepareValidSubmitState(cut.Instance);

        await cut.InvokeAsync(() => ParticipationSelect(cut, "Participation handling").Instance.ValueChanged.InvokeAsync(4));
        await cut.InvokeAsync(() => ParticipationSelect(cut, "Advance registration").Instance.ValueChanged.InvokeAsync(3));
        await cut.InvokeAsync(() => ParticipationSelect(cut, "Identity access").Instance.ValueChanged.InvokeAsync(1));

        await InvokePrivateAsync(cut.Instance, "SaveAsDraftAsync");

        await Assert.That(capturedRequest).IsNotNull();
        var configuration = capturedRequest!.ParticipationConfiguration;
        await Assert.That(configuration.ParticipationHandlingModeId).IsEqualTo(4);
        await Assert.That(configuration.AdvanceRegistrationObligationId).IsEqualTo(3);
        await Assert.That(configuration.IdentityAccessModeId).IsEqualTo(1);
        await Assert.That(configuration.GuestRecoveryPolicy).IsNull();
    }

    [Test]
    public async Task SaveDraft_WithPlatformGuestParticipation_UsesSelectedTypedValues()
    {
        CreateEventDraftRequestDto? capturedRequest = null;
        _eventService.CreateEventAsync(Arg.Do<CreateEventDraftRequestDto>(dto => capturedRequest = dto))
            .Returns(new BaseCommandResponseOfGuid { Success = true, Id = Guid.NewGuid() });
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForElement("[data-testid='participation-handling-select']");
        PrepareValidSubmitState(cut.Instance);

        await cut.InvokeAsync(() => ParticipationSelect(cut, "Participation handling").Instance.ValueChanged.InvokeAsync(4));
        await cut.InvokeAsync(() => ParticipationSelect(cut, "Identity access").Instance.ValueChanged.InvokeAsync(2));
        await cut.InvokeAsync(() => ParticipationSelect(cut, "Guest recovery").Instance.ValueChanged.InvokeAsync(1));

        await InvokePrivateAsync(cut.Instance, "SaveAsDraftAsync");

        await Assert.That(capturedRequest).IsNotNull();
        var configuration = capturedRequest!.ParticipationConfiguration;
        await Assert.That(configuration.ParticipationHandlingModeId).IsEqualTo(4);
        await Assert.That(configuration.AdvanceRegistrationObligationId).IsEqualTo(2);
        await Assert.That(configuration.IdentityAccessModeId).IsEqualTo(2);
        await Assert.That(configuration.GuestRecoveryPolicy).IsEqualTo(GuestRecoveryPolicyEnum.UnverifiedEmailAccepted);
    }

    [Test]
    public async Task ParticipationChanges_ClearValuesThatAreIllegalForTheSelectedMode()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForElement("[data-testid='participation-handling-select']");

        await cut.InvokeAsync(() => ParticipationSelect(cut, "Participation handling").Instance.ValueChanged.InvokeAsync(4));
        await cut.InvokeAsync(() => ParticipationSelect(cut, "Identity access").Instance.ValueChanged.InvokeAsync(2));
        await cut.InvokeAsync(() => ParticipationSelect(cut, "Guest recovery").Instance.ValueChanged.InvokeAsync(3));
        await cut.InvokeAsync(() => ParticipationSelect(cut, "Participation handling").Instance.ValueChanged.InvokeAsync(3));

        await Assert.That(ParticipationSelect(cut, "Advance registration").Instance.Value).IsEqualTo(2);
        await Assert.That(cut.FindComponents<MudSelect<int?>>().Any(item => item.Instance.Label == "Identity access")).IsFalse();
        await Assert.That(cut.FindComponents<MudSelect<int?>>().Any(item => item.Instance.Label == "Guest recovery")).IsFalse();

        await cut.InvokeAsync(() => ParticipationSelect(cut, "Participation handling").Instance.ValueChanged.InvokeAsync(4));

        await Assert.That(ParticipationSelect(cut, "Identity access").Instance.Value).IsNull();
        await Assert.That(cut.FindComponents<MudSelect<int?>>().Any(item => item.Instance.Label == "Guest recovery")).IsFalse();

        await cut.InvokeAsync(() => ParticipationSelect(cut, "Participation handling").Instance.ValueChanged.InvokeAsync(2));

        await Assert.That(ParticipationSelect(cut, "Advance registration").Instance.Value).IsEqualTo(1);
        await Assert.That(ParticipationSelect(cut, "Advance registration").Instance.Disabled).IsTrue();
    }

    [Test]
    public async Task SaveDraft_WithRegistrationPolicy_KeepsParticipationIndependent()
    {
        CreateEventDraftRequestDto? capturedRequest = null;
        _eventService.CreateEventAsync(Arg.Do<CreateEventDraftRequestDto>(dto => capturedRequest = dto))
            .Returns(new BaseCommandResponseOfGuid { Success = true, Id = Guid.NewGuid() });
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForElement("[data-testid='participation-handling-select']");
        PrepareValidSubmitState(cut.Instance);
        await cut.InvokeAsync(() => ParticipationSelect(cut, "Registration scope policy").Instance.ValueChanged.InvokeAsync(3));

        await InvokePrivateAsync(cut.Instance, "SaveAsDraftAsync");

        await Assert.That(capturedRequest).IsNotNull();
        await Assert.That(capturedRequest!.RegistrationPolicyId).IsEqualTo(3);
        await Assert.That(capturedRequest.ParticipationConfiguration.ParticipationHandlingModeId).IsEqualTo(1);
        await Assert.That(capturedRequest.ParticipationConfiguration.AdvanceRegistrationObligationId).IsEqualTo(1);
        await Assert.That(capturedRequest.ParticipationConfiguration.IdentityAccessModeId).IsNull();
        await Assert.That(capturedRequest.ParticipationConfiguration.GuestRecoveryPolicy).IsNull();
    }

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
        var templates = GetPrivateField<IReadOnlyList<HalResourceOfEventTemplateListDto>>(cut.Instance, "eventTemplates");
        var selectedDetail = GetPrivateField<HalResourceOfEventTemplateDto?>(cut.Instance, "_selectedEventTemplate");

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
            .Returns((HalResourceOfEventTemplateDto?)null);

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
        var slowPreview = new TaskCompletionSource<HalResourceOfEventTemplateDto?>(TaskCreationOptions.RunContinuationsAsynchronously);
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
        var selectedDetail = GetPrivateField<HalResourceOfEventTemplateDto?>(cut.Instance, "_selectedEventTemplate");

        await Assert.That(dto.TemplateId).IsEqualTo(fastTemplateId);
        await Assert.That(selectedDetail?.Id).IsEqualTo(fastTemplateId);
    }

    [Test]
    public async Task CreateEvent_WhenTemplateListRequestsRace_KeepsLatestEventTypeTemplatesOnly()
    {
        // Arrange
        var slowTemplate = CreateTemplateListModel(Guid.NewGuid(), "Conference Template", eventTypeId: 1);
        var fastTemplate = CreateTemplateListModel(Guid.NewGuid(), "Workshop Template", eventTypeId: 2);
        var slowTemplates = new TaskCompletionSource<HalCollectionResourceOfEventTemplateListDto>(TaskCreationOptions.RunContinuationsAsynchronously);

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
        var templates = GetPrivateField<IReadOnlyList<HalResourceOfEventTemplateListDto>>(cut.Instance, "eventTemplates");

        await Assert.That(dto.EventTypeId).IsEqualTo(2);
        await Assert.That(templates.Count).IsEqualTo(1);
        await Assert.That(templates[0].Id).IsEqualTo(fastTemplate.Id);
    }

    [Test]
    public async Task CreateEvent_WhenImageReadThrows_ShowsGenericUploadError()
    {
        var rawError = "provider secret body https://upload.example.com/object?signature=abc";
        var file = Substitute.For<IBrowserFile>();
        file.Name.Returns(@"..\..\secret<script>.png");
        file.Size.Returns(1024L);
        file.ContentType.Returns("image/png");
        _imageStorageService.ReadFileAsync(file, Arg.Any<long>())
            .ThrowsAsync(new InvalidOperationException(rawError));
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var cut = _ctx.RenderMudComponent<CreateEvent>();

        await InvokePrivateAsync(cut.Instance, "OnImageFileSelected", file);

        var uploadError = GetPrivateField<string?>(cut.Instance, "_uploadError");
        await Assert.That(uploadError).IsEqualTo(ImageUploadClientPolicy.GenericUploadFailureMessage);
        await Assert.That(uploadError).DoesNotContain(rawError);
    }

    [Test]
    public async Task CreateEvent_WhenTemplatePreviewIsLoading_DoesNotSubmitTemplateId()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var preview = new TaskCompletionSource<HalResourceOfEventTemplateDto?>(TaskCreationOptions.RunContinuationsAsynchronously);
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
        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "HandleSubmit"));

        // Assert
        await _eventService.DidNotReceive().CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>());
        var error = GetSubmitError(cut.Instance);
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
        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "HandleSubmit"));

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
        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "HandleSubmit"));

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
        _eventService.CreateSessionAsync(Arg.Any<Explore.Blazor.Client.Clients.CreateEventSessionDto>()).Returns(new BaseCommandResponseOfGuid
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
        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "HandleSubmit"));

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
        await _categoryService.Received().GetAllCategoriesAsync();
        await _tagService.Received().GetAllTagsAsync();
        await _locationService.DidNotReceive().GetAllLocationsAsync();
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

        SetPrivateField(cut.Instance, "_publisherMode", "organization");
        await InvokePrivateAsync(cut.Instance, "OnPublisherSelectionChanged", "personal");
        await Assert.That(GetPrivateField<string>(cut.Instance, "_publisherMode")).IsEqualTo("organization");

        SetPrivateField(cut.Instance, "_publisherMode", "personal");
        PrepareValidSubmitState(cut.Instance);

        // Act
        await InvokePrivateAsync(cut.Instance, "HandleSubmit");

        // Assert
        await _eventService.DidNotReceive().CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>());
        var error = GetSubmitError(cut.Instance);
        await Assert.That(error).Contains("No available publisher");
    }

    [Test]
    public async Task CreateEvent_WhenContextAuthorizesOrganization_RendersServerPublisherOption()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForElement(".create-event__publisher-selector");

        var context = await _eventService.GetEventCreationContextAsync(CancellationToken.None);
        var organizationId = context.PublisherOptions!.Single(option => option.PublisherMode == "organization").PublisherId;
        await InvokePrivateAsync(cut.Instance, "OnPublisherSelectionChanged", $"organization:{organizationId:D}");

        await Assert.That(GetPrivateField<string>(cut.Instance, "_publisherMode")).IsEqualTo("organization");
        await Assert.That(GetPrivateField<Guid?>(cut.Instance, "_selectedOrganizationId")).IsEqualTo(organizationId);
    }

    [Test]
    public async Task CreateEvent_WhenContextDeniesOrganization_DoesNotSelectOrSubmit()
    {
        var organizationId = Guid.NewGuid();
        _eventService.GetEventCreationContextAsync(Arg.Any<CancellationToken>()).Returns(new EventCreationContextDto
        {
            CanCreate = true,
            PublisherOptions = new List<EventCreationPublisherOptionDto>
            {
                new() { PublisherMode = "personal", DisplayName = "Personal profile", CanPublish = true },
                new()
                {
                    PublisherMode = "organization",
                    PublisherId = organizationId,
                    DisplayName = "Organization with local admin data",
                    CanPublish = false,
                    Reason = "Organization publishing is unavailable."
                }
            }
        });
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForElement(".create-event__publisher-selector");

        await InvokePrivateAsync(cut.Instance, "OnPublisherSelectionChanged", $"organization:{organizationId:D}");
        await Assert.That(GetPrivateField<string>(cut.Instance, "_publisherMode")).IsEqualTo("personal");

        SetPrivateField(cut.Instance, "_publisherMode", "organization");
        SetPrivateField<Guid?>(cut.Instance, "_selectedOrganizationId", organizationId);
        PrepareValidSubmitState(cut.Instance);

        await InvokePrivateAsync(cut.Instance, "HandleSubmit");

        await _eventService.DidNotReceive().CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>());
        await Assert.That(GetSubmitError(cut.Instance)).Contains("Select an available publisher");
    }

    [Test]
    public async Task CreateEvent_WhenContextFails_RendersNoPublisherOptionsAndBlocksForcedPersonalSubmission()
    {
        _eventService.GetEventCreationContextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<EventCreationContextDto>(new HttpRequestException("Unavailable")));
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Creation permissions could not be loaded. Publishing is unavailable.", StringComparison.Ordinal)
                || cut.Markup.Contains("Select Organization", StringComparison.Ordinal)
                || cut.Markup.Contains("Create Organization", StringComparison.Ordinal)
                || cut.Markup.Contains("Personal profile", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Creation-context failure exposed a publisher fallback.");
            }
        }, TimeSpan.FromSeconds(3));

        SetPrivateField(cut.Instance, "_publisherMode", "personal");
        PrepareValidSubmitState(cut.Instance);

        await InvokePrivateAsync(cut.Instance, "HandleSubmit");

        await _eventService.DidNotReceive().CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>());
        await Assert.That(GetSubmitError(cut.Instance)).Contains("Creation permissions could not be loaded");
    }

    [Test]
    public async Task CreateEvent_WhenContextIsNull_RendersNoPublisherOptionsAndBlocksSubmission()
    {
        _eventService.GetEventCreationContextAsync(Arg.Any<CancellationToken>()).Returns((EventCreationContextDto?)null!);
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Creation permissions could not be loaded. Publishing is unavailable.", StringComparison.Ordinal)
                || cut.Markup.Contains("Personal profile", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A null creation context exposed a publisher option.");
            }
        }, TimeSpan.FromSeconds(3));

        PrepareValidSubmitState(cut.Instance);

        await InvokePrivateAsync(cut.Instance, "HandleSubmit");

        await _eventService.DidNotReceive().CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>());
        await Assert.That(GetSubmitError(cut.Instance)).Contains("Creation permissions could not be loaded");
    }

    [Test]
    public async Task ManualEvidence_CapturesSanitizedPublisherContextStates()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var available = _ctx.RenderMudComponent<CreateEvent>();
        available.WaitForElement(".create-event__publisher-selector");
        var availableContext = await _eventService.GetEventCreationContextAsync(CancellationToken.None);
        var organizationId = availableContext.PublisherOptions!.Single(option => option.PublisherMode == "organization").PublisherId;
        await InvokePrivateAsync(available.Instance, "OnPublisherSelectionChanged", $"organization:{organizationId:D}");
        var availableMode = GetPrivateField<string>(available.Instance, "_publisherMode");
        var availableMarkup = available.Markup;
        available.Dispose();

        using var deniedFixture = new CreateEventTests();
        deniedFixture._eventService.GetEventCreationContextAsync(Arg.Any<CancellationToken>()).Returns(new EventCreationContextDto
        {
            CanCreate = true,
            PublisherOptions =
            [
                new() { PublisherMode = "personal", DisplayName = "Personal profile", CanPublish = true },
                new()
                {
                    PublisherMode = "organization",
                    PublisherId = organizationId,
                    DisplayName = "Test Organization",
                    CanPublish = false,
                    Reason = "Organization publishing is unavailable."
                }
            ]
        });
        deniedFixture._ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var denied = deniedFixture._ctx.RenderMudComponent<CreateEvent>();
        denied.WaitForElement(".create-event__publisher-selector");
        await InvokePrivateAsync(denied.Instance, "OnPublisherSelectionChanged", $"organization:{organizationId:D}");
        var deniedMode = GetPrivateField<string>(denied.Instance, "_publisherMode");
        var deniedMarkup = denied.Markup;
        denied.Dispose();

        using var missingFixture = new CreateEventTests();
        missingFixture._eventService.GetEventCreationContextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<EventCreationContextDto>(new HttpRequestException("Unavailable")));
        missingFixture._ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var missing = missingFixture._ctx.RenderMudComponent<CreateEvent>();
        missing.WaitForAssertion(() =>
        {
            if (!missing.Markup.Contains("Creation permissions could not be loaded. Publishing is unavailable.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Creation-context failure was not rendered.");
            }
        }, TimeSpan.FromSeconds(3));
        await Assert.That(missing.Markup).DoesNotContain("Personal profile");

        var artifact = $"""
            <!doctype html>
            <html lang="en"><body>
            <h1>Create Event publisher-context bUnit manual QA</h1>
            <section data-scenario="context-available" data-selected-mode="{availableMode}">{availableMarkup}</section>
            <section data-scenario="context-denied" data-selected-mode="{deniedMode}">{deniedMarkup}</section>
            <section data-scenario="context-missing" data-publisher-options="none" data-submit-disabled="true">{missing.Markup}</section>
            </body></html>
            """;
        artifact = System.Text.RegularExpressions.Regex.Replace(
            artifact,
            "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
            "[redacted-guid]");
        artifact = System.Text.RegularExpressions.Regex.Replace(artifact, "[\\w.+-]+@[\\w.-]+", "[redacted-email]");

        await Assert.That(artifact).Contains("data-selected-mode=\"organization\"");
        await Assert.That(artifact).Contains("data-selected-mode=\"personal\"");
        await Assert.That(artifact).Contains("Creation permissions could not be loaded. Publishing is unavailable.");
        await Assert.That(artifact).Contains("data-publisher-options=\"none\"");
        await Assert.That(artifact).Contains("data-submit-disabled=\"true\"");
        await Assert.That(artifact).DoesNotContain("Select Organization");
        await Assert.That(artifact).DoesNotContain("Create Organization");

        var evidenceDirectory = Environment.GetEnvironmentVariable("PHASE2_TASK22_UI_EVIDENCE_DIR")
            ?? Path.Combine(Directory.GetCurrentDirectory(), ".omo", "evidence", "phase2-task22-create-event");
        Directory.CreateDirectory(evidenceDirectory);
        await File.WriteAllTextAsync(Path.Combine(evidenceDirectory, "publisher-context-states.html"), artifact);
    }

    [Test]
    public async Task CreateEvent_WhenReviewPublishFindsReadinessErrors_SavesDraftAndShowsErrors()
    {
        // Arrange
        var readinessError = "At least one scheduled session is required before publishing.";
        _eventService.CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>()).Returns(new BaseCommandResponseOfGuid
        {
            Success = false,
            Errors = new List<string> { readinessError }
        });
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));
        PrepareValidSubmitState(cut.Instance);
        SetPrivateField<DateTime?>(cut.Instance, "_inlineSessionDate", new DateTime(2026, 7, 10));
        SetPrivateField<TimeSpan?>(cut.Instance, "_inlineSessionStartTime", TimeSpan.FromHours(10));
        SetPrivateField<TimeSpan?>(cut.Instance, "_inlineSessionEndTime", TimeSpan.FromHours(12));

        // Act
        await SubmitReviewAndPublishAsync(cut.Instance);

        // Assert
        await _eventService.Received(1).CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>());
        await _eventService.DidNotReceive().GetEventPublishReadinessAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _eventService.DidNotReceive().PublishEventAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        var error = GetSubmitError(cut.Instance);
        await Assert.That(error).Contains(readinessError);
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
    public async Task HandleSubmit_WithPreUploadedImage_SendsFeaturedImageIdAndPublishesWhenReady()
    {
        var createdEventId = Guid.NewGuid();
        var uploadedImageId = Guid.NewGuid();
        CreateEventDraftRequestDto? capturedRequest = null;
        _eventService.CreateEventAsync(Arg.Do<CreateEventDraftRequestDto>(dto => capturedRequest = dto)).Returns(new BaseCommandResponseOfGuid
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
        SetPrivateField<Guid?>(cut.Instance, "_uploadedImageStorageObjectId", uploadedImageId);
        SetPrivateField<DateTime?>(cut.Instance, "_inlineSessionDate", new DateTime(2026, 7, 10));
        SetPrivateField<TimeSpan?>(cut.Instance, "_inlineSessionStartTime", TimeSpan.FromHours(10));
        SetPrivateField<TimeSpan?>(cut.Instance, "_inlineSessionEndTime", TimeSpan.FromHours(12));

        await InvokePrivateAsync(cut.Instance, "HandleSubmit");

        await Assert.That(capturedRequest).IsNotNull();
        await Assert.That(capturedRequest!.FeaturedImageId).IsEqualTo(uploadedImageId);
        await Assert.That(capturedRequest.EventStatusId).IsEqualTo(2);
        await _eventService.DidNotReceive().GetEventPublishReadinessAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _eventService.DidNotReceive().PublishEventAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        var navigation = _ctx.Services.GetRequiredService<NavigationManager>();
        await Assert.That(navigation.Uri).EndsWith($"/event-created/{createdEventId}");
    }

    [Test]
    public async Task HandleSubmit_WithInlineSession_PopulatesSessionsAndPublishesWhenReady()
    {
        var createdEventId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        CreateEventDraftRequestDto? capturedRequest = null;
        _eventService.CreateEventAsync(Arg.Do<CreateEventDraftRequestDto>(dto => capturedRequest = dto)).Returns(new BaseCommandResponseOfGuid
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
        SetPrivateField<DateTime?>(cut.Instance, "_inlineSessionDate", new DateTime(2026, 7, 10));
        SetPrivateField<TimeSpan?>(cut.Instance, "_inlineSessionStartTime", TimeSpan.FromHours(10));
        SetPrivateField<TimeSpan?>(cut.Instance, "_inlineSessionEndTime", TimeSpan.FromHours(12));
        SetPrivateField<Guid?>(cut.Instance, "_inlineSessionLocationId", locationId);
        SetPrivateField<int?>(cut.Instance, "_inlineSessionCapacity", 120);

        await InvokePrivateAsync(cut.Instance, "HandleSubmit");

        await Assert.That(capturedRequest).IsNotNull();
        await Assert.That(capturedRequest!.EventStatusId).IsEqualTo(2);
        await Assert.That(capturedRequest.Sessions).HasSingleItem();
        var session = capturedRequest.Sessions!.Single();
        await Assert.That(session.LocationId).IsEqualTo(locationId);
        await Assert.That(session.MaxAudienceAttendees).IsEqualTo(120);
        await Assert.That(session.StartTime).IsNotNull();
        await Assert.That(session.EndTime).IsNotNull();
        await Assert.That(session.EndTime!.Value).IsGreaterThan(session.StartTime!.Value);
        await _eventService.DidNotReceive().GetEventPublishReadinessAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _eventService.DidNotReceive().PublishEventAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        var navigation = _ctx.Services.GetRequiredService<NavigationManager>();
        await Assert.That(navigation.Uri).EndsWith($"/event-created/{createdEventId}");
    }

    [Test]
    public async Task HandleSubmit_WithValidationProblemDetails_MapsServerErrorsIntoEditContext()
    {
        _eventService.CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>())
            .ThrowsAsync(new ApiException<ValidationProblemDetails>(
                "Bad Request",
                400,
                string.Empty,
                new Dictionary<string, IEnumerable<string>>(),
                new ValidationProblemDetails
                {
                    Errors = new Dictionary<string, ICollection<string>>
                    {
                        ["Title.Value"] = new[] { "Use a clearer event title." }
                    }
                },
                null));
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));
        PrepareValidSubmitState(cut.Instance);
        SetPrivateField<DateTime?>(cut.Instance, "_inlineSessionDate", new DateTime(2026, 7, 10));
        SetPrivateField<TimeSpan?>(cut.Instance, "_inlineSessionStartTime", TimeSpan.FromHours(10));
        SetPrivateField<TimeSpan?>(cut.Instance, "_inlineSessionEndTime", TimeSpan.FromHours(12));

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "HandleSubmit"));

        await Assert.That(GetSubmitError(cut.Instance)).IsEqualTo("Please fix the validation errors below.");
        await Assert.That(GetValidationMessages(cut.Instance)).Contains("Use a clearer event title.");
    }

    [Test]
    public async Task HandleSubmit_WithUnexpectedException_DoesNotEchoRawExceptionMessage()
    {
        const string rawProviderMessage = "provider rejected <script>alert(1)</script> secret";
        _eventService.CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>())
            .ThrowsAsync(new InvalidOperationException(rawProviderMessage));
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));
        PrepareValidSubmitState(cut.Instance);
        SetPrivateField<DateTime?>(cut.Instance, "_inlineSessionDate", new DateTime(2026, 7, 10));
        SetPrivateField<TimeSpan?>(cut.Instance, "_inlineSessionStartTime", TimeSpan.FromHours(10));
        SetPrivateField<TimeSpan?>(cut.Instance, "_inlineSessionEndTime", TimeSpan.FromHours(12));

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "HandleSubmit"));

        var submitError = GetSubmitError(cut.Instance);
        await Assert.That(submitError).IsEqualTo("Event could not be submitted. Please try again.");
        await Assert.That(submitError).DoesNotContain(rawProviderMessage);
        await Assert.That(submitError).DoesNotContain("<script>");
    }

    [Test]
    public async Task CreateEvent_WhenReviewPublishIsReady_PublishesDraftWithConcurrencyStampWithoutDialog()
    {
        // Arrange
        var createdEventId = Guid.NewGuid();
        CreateEventDraftRequestDto? capturedRequest = null;
        _eventService.CreateEventAsync(Arg.Do<CreateEventDraftRequestDto>(dto => capturedRequest = dto)).Returns(new BaseCommandResponseOfGuid
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
        SetPrivateField<DateTime?>(cut.Instance, "_inlineSessionDate", new DateTime(2026, 7, 10));
        SetPrivateField<TimeSpan?>(cut.Instance, "_inlineSessionStartTime", TimeSpan.FromHours(10));
        SetPrivateField<TimeSpan?>(cut.Instance, "_inlineSessionEndTime", TimeSpan.FromHours(12));

        // Act
        await SubmitReviewAndPublishAsync(cut.Instance);

        // Assert
        await _eventService.Received(1).CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>());
        await Assert.That(capturedRequest).IsNotNull();
        await Assert.That(capturedRequest!.EventStatusId).IsEqualTo(2);
        await _dialogService.DidNotReceive().ShowMessageBoxAsync(
            "Review and publish",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DialogOptions>());
        await _eventService.DidNotReceive().GetEventPublishReadinessAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _eventService.DidNotReceive().PublishEventAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        var navigation = _ctx.Services.GetRequiredService<NavigationManager>();
        await Assert.That(navigation.Uri).EndsWith($"/event-created/{createdEventId}");
    }

    [Test]
    public async Task CreateEvent_WhenDialogServiceWouldCancel_PublishesWithoutOpeningDialog()
    {
        // Arrange
        var createdEventId = Guid.NewGuid();
        CreateEventDraftRequestDto? capturedRequest = null;
        _eventService.CreateEventAsync(Arg.Do<CreateEventDraftRequestDto>(dto => capturedRequest = dto)).Returns(new BaseCommandResponseOfGuid
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
            .Returns(false);
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();
        cut.WaitForState(() => cut.Markup.Contains("mud-alert", StringComparison.OrdinalIgnoreCase)
                              || cut.Markup.Contains("mud-input", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));
        PrepareValidSubmitState(cut.Instance);
        SetPrivateField<DateTime?>(cut.Instance, "_inlineSessionDate", new DateTime(2026, 7, 10));
        SetPrivateField<TimeSpan?>(cut.Instance, "_inlineSessionStartTime", TimeSpan.FromHours(10));
        SetPrivateField<TimeSpan?>(cut.Instance, "_inlineSessionEndTime", TimeSpan.FromHours(12));

        // Act
        await SubmitReviewAndPublishAsync(cut.Instance);

        // Assert
        await _eventService.Received(1).CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>());
        await Assert.That(capturedRequest).IsNotNull();
        await Assert.That(capturedRequest!.EventStatusId).IsEqualTo(2);
        await _eventService.DidNotReceive().GetEventPublishReadinessAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _eventService.DidNotReceive().GetEventByIdAsync(Arg.Any<Guid>());
        await _dialogService.DidNotReceive().ShowMessageBoxAsync(
            "Review and publish",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DialogOptions>());
        await _eventService.DidNotReceive().PublishEventAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        var navigation = _ctx.Services.GetRequiredService<NavigationManager>();
        await Assert.That(navigation.Uri).EndsWith($"/event-created/{createdEventId}");
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
    public async Task CreateEvent_ShowsPublishAction()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();

        cut.WaitForAssertion(() =>
        {
            var buttons = cut.FindAll("button");
            var hasPublishButton = buttons.Any(b => b.TextContent.Contains("Publish", StringComparison.OrdinalIgnoreCase));
            if (!hasPublishButton)
            {
                throw new InvalidOperationException("Publish action was not rendered.");
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
            if (!cut.Markup.Contains("Event schedule", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Date and time", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Set up multiple sessions", StringComparison.OrdinalIgnoreCase)
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

            if (!cut.Markup.Contains("Visibility, audience, participation, registration, classification, categories, and tags.", StringComparison.OrdinalIgnoreCase)
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
    public async Task CreateEvent_ThemeQuickBarUpdatesPagePreviewStyle()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Sage", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Soft", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Theme quick bar presets and effects were not rendered.");
            }
        }, TimeSpan.FromSeconds(3));

        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Sage", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Soft", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.WaitForAssertion(() =>
        {
            var style = _ctx.Services.GetRequiredService<MainContentAppearanceState>().Style;

            if (string.IsNullOrWhiteSpace(style)
                || !style.Contains("background: #5D7661;", StringComparison.OrdinalIgnoreCase)
                || !style.Contains("background-image: linear-gradient(rgba(0,0,0,0.24), rgba(0,0,0,0.24));", StringComparison.OrdinalIgnoreCase)
                || !style.Contains("--event-theme-text-color:", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Create Event did not publish the selected theme preview style to the main content canvas.");
            }
        }, TimeSpan.FromSeconds(3));
    }

    [Test]
    public async Task CreateEvent_RendersEventScheduleBeforeEventSettings()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Event schedule", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("A single scheduled session is created with the event by default.", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Set up multiple sessions", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Date and time information", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Event schedule section was not rendered with the expected single-session guidance.");
            }

            if (cut.Markup.Contains("child event", StringComparison.OrdinalIgnoreCase)
                || cut.Markup.Contains("subevent", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Program Summary must not reintroduce child-event language.");
            }
        }, TimeSpan.FromSeconds(3));

        await Assert.That(cut.Markup.IndexOf("Event schedule", StringComparison.OrdinalIgnoreCase))
            .IsLessThan(cut.Markup.IndexOf("Event settings", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task CreateEvent_DoesNotRenderScheduleLogisticsComposerInEventShell()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.RenderMudComponent<CreateEvent>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Event schedule", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Set up multiple sessions", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Inline event schedule was not rendered after removing the schedule composer.");
            }

            var forbiddenCopies = new[]
            {
                "Schedule timeline composer",
                "Open schedule timeline",
                "Add to this day",
                "Add another session",
                "Scheduling (Days, Rooms & Agenda)",
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

    private static HalCollectionResourceOfEventTemplateListDto CreateTemplatePage(params HalResourceOfEventTemplateListDto[] templates) => new()
    {
        _embedded = new HalCollectionEmbeddedOfEventTemplateListDto { Items = templates.ToList() },
        PageNumber = 1,
        PageSize = 100,
        TotalCount = templates.Length,
        TotalPages = templates.Length == 0 ? 0 : 1
    };

    private static HalResourceOfEventTemplateListDto CreateTemplateListModel(Guid id, string displayName, int eventTypeId) => new()
    {
        Id = id,
        TemplateKey = displayName.ToLowerInvariant().Replace(' ', '-'),
        DisplayName = displayName,
        EventTypeId = eventTypeId,
        Version = 1,
        IsActive = true,
        IsPublished = true,
        DefinitionCount = 1
    };

    private static HalResourceOfEventTemplateDto CreateTemplateDetailModel(Guid id, string displayName, int eventTypeId) => new()
    {
        Id = id,
        TemplateKey = displayName.ToLowerInvariant().Replace(' ', '-'),
        DisplayName = displayName,
        EventTypeId = eventTypeId,
        Version = 1,
        IsActive = true,
        IsPublished = true,
        Definitions = new List<Definitions4>
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

        if (result is null)
        {
            return;
        }

        throw new InvalidOperationException($"Private method {methodName} returned an unsupported result.");
    }

    private static Task SubmitReviewAndPublishAsync(CreateEvent component)
    {
        var intentType = typeof(CreateEvent).GetNestedType("CreateEventSubmitIntent", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CreateEventSubmitIntent was not found.");

        var intent = Enum.Parse(intentType, "ReviewAndPublish");
        return InvokePrivateAsync(component, "SubmitEventAsync", intent);
    }

    private static T GetPrivateField<T>(CreateEvent component, string fieldName)
    {
        var field = typeof(CreateEvent).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(CreateEvent).FullName, fieldName);

        return (T)field.GetValue(component)!;
    }

    private static IRenderedComponent<MudSelect<int?>> ParticipationSelect(
        IRenderedComponent<CreateEvent> cut,
        string label) => cut.FindComponents<MudSelect<int?>>().Single(item => item.Instance.Label == label);

    private static string GetSubmitError(CreateEvent component)
    {
        var submitState = GetPrivateField<Explore.Blazor.Client.Components.Forms.FormSubmitState>(component, "_submitState");
        return submitState.ErrorMessage ?? throw new InvalidOperationException("Submit state error message was not set.");
    }

    private static IReadOnlyList<string> GetValidationMessages(CreateEvent component)
    {
        var editContext = GetPrivateField<EditContext>(component, "_editContext");
        return editContext.GetValidationMessages().ToList();
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
