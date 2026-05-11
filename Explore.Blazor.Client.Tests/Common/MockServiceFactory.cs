// ABOUTME: Factory for creating pre-configured mock services for common testing scenarios.
// All mocks use NSubstitute and return sensible defaults using HAL resource types.

namespace Explore.Blazor.Client.Tests.Common;

/// <summary>
/// Factory for creating pre-configured mock services for common testing scenarios.
/// All mocks use NSubstitute and return sensible defaults.
/// </summary>
/// <remarks>
/// This factory follows enterprise testing best practices:
/// - All user IDs are Guid (matching domain model) but converted to string for JWT claim compatibility
/// - All tenant IDs are Guid (matching domain model)
/// - Default responses are empty collections (fail-safe)
/// - Mocks are configured for common success scenarios
/// - Uses HAL resource types matching the actual API client interface
/// </remarks>
public static class MockServiceFactory
{
    #region Core API Client

    /// <summary>
    /// Creates a mock IEventApiClient with default empty responses.
    /// Uses HAL resource types that match the actual NSwag-generated client.
    /// </summary>
    public static IEventApiClient CreateEventApiClient()
    {
        var mock = Substitute.For<IEventApiClient>();

        // Configure default successful empty responses for events (HAL collection)
        // GetEventsAsync has many optional filter parameters (searchTerm, categoryId, etc.)
        // Use ReturnsForAnyArgs to match regardless of which parameters are passed
        mock.GetEventsAsync().ReturnsForAnyArgs(new HalCollectionResourceOfEventListDto
        {
            _embedded = new HalCollectionEmbeddedOfEventListDto
            {
                Items = new List<HalResourceOfEventListDto>()
            }
        });

        // Configure default successful empty responses for my events (HAL collection)
        mock.GetMyEventsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new HalCollectionResourceOfEventListDto
            {
                _embedded = new HalCollectionEmbeddedOfEventListDto
                {
                    Items = new List<HalResourceOfEventListDto>()
                }
            });

        // Configure default successful empty responses for organizations (HAL collection)
        mock.GetOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new HalCollectionResourceOfOrganizationListDto
            {
                _embedded = new HalCollectionEmbeddedOfOrganizationListDto
                {
                    Items = new List<HalResourceOfOrganizationListDto>()
                }
            });

        // Configure default successful empty responses for my organizations (HAL collection)
        mock.GetMyOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new HalCollectionResourceOfOrganizationListDto
            {
                _embedded = new HalCollectionEmbeddedOfOrganizationListDto
                {
                    Items = new List<HalResourceOfOrganizationListDto>()
                }
            });

        return mock;
    }

    #endregion

    #region Service Mocks

    /// <summary>
    /// Creates a mock IEventService with default empty responses.
    /// </summary>
    public static IEventService CreateEventService()
    {
        var mock = Substitute.For<IEventService>();
        mock.GetAllEventsAsync().Returns(new List<EventListDto>());
        mock.GetMyEventsAsync().Returns(new List<EventListDto>());
        return mock;
    }

    /// <summary>
    /// Creates a mock IOrganizationService with default empty responses.
    /// </summary>
    public static IOrganizationService CreateOrganizationService()
    {
        var mock = Substitute.For<IOrganizationService>();
        mock.GetMyOrganizationsAsync().Returns(new List<OrganizationListDto>());
        return mock;
    }

    /// <summary>
    /// Creates a mock IGroupService with default empty responses.
    /// </summary>
    public static IGroupService CreateGroupService()
    {
        var mock = Substitute.For<IGroupService>();
        mock.GetMyGroupsAsync().Returns(new List<GroupPublisherListDto>());
        return mock;
    }

    /// <summary>
    /// Creates a mock IAuthStateService with authenticated user defaults.
    /// </summary>
    /// <param name="userId">User ID as Guid (converted to string for JWT claim compatibility)</param>
    /// <param name="tenantId">Tenant ID (uses default if null)</param>
    /// <remarks>
    /// The userId parameter is Guid because that's the domain model type.
    /// Internally, GetCurrentUserIdAsync() returns string because JWT claims are strings.
    /// This design bridges domain model expectations with JWT reality.
    /// </remarks>
    public static IAuthStateService CreateAuthStateService(Guid? userId = null, Guid? tenantId = null)
    {
        var mock = Substitute.For<IAuthStateService>();

        // Convert Guid to string (matching IAuthStateService.GetCurrentUserIdAsync signature)
        // The interface returns string because JWT claims are strings
        var userIdValue = userId ?? Guid.NewGuid();
        mock.GetCurrentUserIdAsync().Returns(userIdValue.ToString());

        // TenantId is already Guid in the interface
        mock.GetCurrentTenantIdAsync().Returns(tenantId ?? Guid.Parse("018e4e5c-7f00-7000-8000-000000000001"));
        mock.IsAuthenticatedAsync().Returns(true);

        return mock;
    }

    /// <summary>
    /// Creates a mock IAuthStateService for unauthenticated state.
    /// </summary>
    public static IAuthStateService CreateUnauthenticatedAuthStateService()
    {
        var mock = Substitute.For<IAuthStateService>();
        mock.GetCurrentUserIdAsync().ThrowsAsync(new UnauthorizedAccessException("User is not authenticated"));
        mock.GetCurrentTenantIdAsync().ThrowsAsync(new UnauthorizedAccessException("User is not authenticated"));
        mock.IsAuthenticatedAsync().Returns(false);
        return mock;
    }

    /// <summary>
    /// Creates a mock IUserService with default empty responses.
    /// </summary>
    public static IUserService CreateUserService()
    {
        var mock = Substitute.For<IUserService>();
        return mock;
    }

    /// <summary>
    /// Creates a mock ICategoryService with default empty responses.
    /// </summary>
    public static ICategoryService CreateCategoryService()
    {
        var mock = Substitute.For<ICategoryService>();
        mock.GetAllCategoriesAsync().Returns(new List<CategoryListDto>());
        return mock;
    }

    /// <summary>
    /// Creates a mock ITagService with default empty responses.
    /// </summary>
    public static ITagService CreateTagService()
    {
        var mock = Substitute.For<ITagService>();
        mock.GetAllTagsAsync().Returns(new List<TagListDto>());
        return mock;
    }

    /// <summary>
    /// Creates a mock ILocationService with default empty responses.
    /// </summary>
    public static ILocationService CreateLocationService()
    {
        var mock = Substitute.For<ILocationService>();
        mock.GetAllLocationsAsync().Returns(new List<LocationListDto>());
        return mock;
    }

    /// <summary>
    /// Creates a mock IImageStorageService with default responses.
    /// </summary>
    public static IImageStorageService CreateImageStorageService()
    {
        var mock = Substitute.For<IImageStorageService>();
        return mock;
    }

    /// <summary>
    /// Creates a mock ITenantNavigationService with default empty responses.
    /// </summary>
    public static ITenantNavigationService CreateTenantNavigationService()
    {
        var mock = Substitute.For<ITenantNavigationService>();
        mock.GetNavigationLinksAsync().Returns(new List<TenantNavigationLinkDto>());
        return mock;
    }

    #endregion

    #region Lookup Service Mocks

    /// <summary>
    /// Registers all common lookup table service mocks with empty defaults.
    /// Call this when testing components that load lookup data.
    /// </summary>
    /// <param name="services">Service collection to add mocks to</param>
    /// <remarks>
    /// Method names follow the actual interface signatures:
    /// - IAudienceAgeService.GetAudienceAgesAsync()
    /// - IAudienceGenderService.GetAudienceGendersAsync()
    /// - IEventTypeService.GetEventTypesAsync()
    /// - IEventFormatService.GetEventFormatsAsync()
    /// - IEventStatusService.GetEventStatusesAsync()
    /// - ILanguageService.GetLanguagesAsync()
    /// - IMadhabService.GetMadhabsAsync()
    /// </remarks>
    public static void RegisterLookupServiceMocks(IServiceCollection services)
    {
        // Audience services
        var audienceAgeMock = Substitute.For<IAudienceAgeService>();
        audienceAgeMock.GetAudienceAgesAsync().Returns(new List<AudienceAgeListDto>());
        services.AddSingleton(audienceAgeMock);

        var audienceGenderMock = Substitute.For<IAudienceGenderService>();
        audienceGenderMock.GetAudienceGendersAsync().Returns(new List<AudienceGenderListDto>());
        services.AddSingleton(audienceGenderMock);

        // Event metadata services
        var eventTypeMock = Substitute.For<IEventTypeService>();
        eventTypeMock.GetEventTypesAsync().Returns(new List<EventTypeListDto>());
        services.AddSingleton(eventTypeMock);

        var eventFormatMock = Substitute.For<IEventFormatService>();
        eventFormatMock.GetEventFormatsAsync().Returns(new List<EventFormatListDto>());
        services.AddSingleton(eventFormatMock);

        var eventStatusMock = Substitute.For<IEventStatusService>();
        eventStatusMock.GetEventStatusesAsync().Returns(new List<EventStatusListDto>());
        services.AddSingleton(eventStatusMock);

        // Language and cultural services
        var languageMock = Substitute.For<ILanguageService>();
        languageMock.GetLanguagesAsync().Returns(new List<LanguageListDto>());
        services.AddSingleton(languageMock);

        var madhabMock = Substitute.For<IMadhabService>();
        madhabMock.GetMadhabsAsync().Returns(new List<MadhabListDto>());
        services.AddSingleton(madhabMock);

        var registrationPolicyMock = Substitute.For<IEventRegistrationPolicyService>();
        registrationPolicyMock.GetEventRegistrationPoliciesAsync()
            .Returns(new List<EventRegistrationPolicyListDto>());
        services.AddSingleton(registrationPolicyMock);

        var scheduleItemKindMock = Substitute.For<IScheduleItemKindService>();
        scheduleItemKindMock.GetScheduleItemKindsAsync()
            .Returns(new List<ScheduleItemKindListDto>());
        services.AddSingleton(scheduleItemKindMock);

        var registrationScopeMock = Substitute.For<IRegistrationScopeService>();
        registrationScopeMock.GetRegistrationScopesAsync()
            .Returns(new List<RegistrationScopeListDto>());
        services.AddSingleton(registrationScopeMock);
    }

    /// <summary>
    /// Registers lookup services with pre-generated test data.
    /// </summary>
    public static void RegisterLookupServiceMocksWithData(IServiceCollection services, int itemCount = 5)
    {
        var audienceAgeMock = Substitute.For<IAudienceAgeService>();
        audienceAgeMock.GetAudienceAgesAsync().Returns(ComponentDataBuilder.AudienceAgeListDto.Generate(itemCount));
        services.AddSingleton(audienceAgeMock);

        var audienceGenderMock = Substitute.For<IAudienceGenderService>();
        audienceGenderMock.GetAudienceGendersAsync().Returns(ComponentDataBuilder.AudienceGenderListDto.Generate(itemCount));
        services.AddSingleton(audienceGenderMock);

        var eventTypeMock = Substitute.For<IEventTypeService>();
        eventTypeMock.GetEventTypesAsync().Returns(ComponentDataBuilder.EventTypeListDto.Generate(itemCount));
        services.AddSingleton(eventTypeMock);

        var eventFormatMock = Substitute.For<IEventFormatService>();
        eventFormatMock.GetEventFormatsAsync().Returns(ComponentDataBuilder.EventFormatListDto.Generate(itemCount));
        services.AddSingleton(eventFormatMock);

        var eventStatusMock = Substitute.For<IEventStatusService>();
        eventStatusMock.GetEventStatusesAsync().Returns(ComponentDataBuilder.EventStatusListDto.Generate(itemCount));
        services.AddSingleton(eventStatusMock);

        var languageMock = Substitute.For<ILanguageService>();
        languageMock.GetLanguagesAsync().Returns(ComponentDataBuilder.LanguageListDto.Generate(itemCount));
        services.AddSingleton(languageMock);

        var madhabMock = Substitute.For<IMadhabService>();
        madhabMock.GetMadhabsAsync().Returns(ComponentDataBuilder.MadhabListDto.Generate(itemCount));
        services.AddSingleton(madhabMock);

        var registrationPolicyMock = Substitute.For<IEventRegistrationPolicyService>();
        registrationPolicyMock.GetEventRegistrationPoliciesAsync()
            .Returns(new List<EventRegistrationPolicyListDto>());
        services.AddSingleton(registrationPolicyMock);

        var scheduleItemKindMock = Substitute.For<IScheduleItemKindService>();
        scheduleItemKindMock.GetScheduleItemKindsAsync()
            .Returns(new List<ScheduleItemKindListDto>());
        services.AddSingleton(scheduleItemKindMock);

        var registrationScopeMock = Substitute.For<IRegistrationScopeService>();
        registrationScopeMock.GetRegistrationScopesAsync()
            .Returns(new List<RegistrationScopeListDto>());
        services.AddSingleton(registrationScopeMock);
    }

    #endregion

    #region Notification Services

    /// <summary>
    /// Creates a mock INotificationService that returns empty defaults.
    /// </summary>
    public static INotificationService CreateNotificationService()
    {
        var mock = Substitute.For<INotificationService>();
        mock.GetUnreadCountAsync(Arg.Any<int?>()).Returns(0);
        mock.GetNotificationsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool?>(), Arg.Any<int?>())
            .Returns(Blazor.Client.Models.PaginatedResult<NotificationListDto>.Empty());
        mock.MarkAllAsReadAsync().Returns(true);
        mock.MarkAsReadAsync(Arg.Any<Guid>()).Returns(true);
        mock.DeleteAsync(Arg.Any<Guid>()).Returns(true);
        return mock;
    }

    /// <summary>
    /// Creates a mock ITranslationService that returns key-as-value defaults.
    /// </summary>
    public static ITranslationService CreateTranslationService()
    {
        var mock = Substitute.For<ITranslationService>();
        mock.CurrentLanguage.Returns("en");
        mock.T(Arg.Any<string>(), Arg.Any<string?>()).Returns(ci => ci.ArgAt<string>(0));
        mock.GetTranslationsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>());
        mock.GetAvailableLanguagesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<string> { "en" });
        mock.ChangeLanguageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        mock.PreloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        return mock;
    }

    #endregion

    #region Bulk Registration

    /// <summary>
    /// Creates all core services with default mocks for a complete test setup.
    /// </summary>
    /// <param name="services">Service collection to add mocks to</param>
    /// <param name="userId">Optional user ID for auth service</param>
    /// <param name="tenantId">Optional tenant ID for auth service</param>
    public static void RegisterAllCoreMocks(
        IServiceCollection services,
        Guid? userId = null,
        Guid? tenantId = null)
    {
        services.AddSingleton(CreateEventApiClient());
        services.AddSingleton(CreateEventService());
        services.AddSingleton(CreateOrganizationService());
        services.AddSingleton(CreateGroupService());
        services.AddSingleton(CreateAuthStateService(userId, tenantId));
        services.AddSingleton(CreateUserService());
        services.AddSingleton(CreateCategoryService());
        services.AddSingleton(CreateTagService());
        services.AddSingleton(CreateLocationService());
        services.AddSingleton(CreateImageStorageService());
        services.AddSingleton(CreateTenantNavigationService());
        services.AddSingleton(CreateNotificationService());
        services.AddSingleton(CreateTranslationService());
        services.AddSingleton(Substitute.For<IHttpClientFactory>());
        RegisterLookupServiceMocks(services);

        var eventDayMock = Substitute.For<IEventDayService>();
        eventDayMock.GetDaysByEventAsync(Arg.Any<Guid>())
            .Returns(new List<EventDayListDto>());
        services.AddSingleton(eventDayMock);

        var agendaItemMock = Substitute.For<IEventAgendaItemService>();
        agendaItemMock.GetAgendaItemsByEventAsync(Arg.Any<Guid>())
            .Returns(new List<EventAgendaItemListDto>());
        services.AddSingleton(agendaItemMock);

        var locationRoomMock = Substitute.For<ILocationRoomService>();
        locationRoomMock.GetRoomsByLocationAsync(Arg.Any<Guid>())
            .Returns(new List<LocationRoomListDto>());
        services.AddSingleton(locationRoomMock);
    }

    #endregion
}
