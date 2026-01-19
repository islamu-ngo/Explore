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
/// </remarks>
public static class MockServiceFactory
{
    #region Core API Client

    /// <summary>
    /// Creates a mock IEventApiClient with default empty responses.
    /// </summary>
    public static IEventApiClient CreateEventApiClient()
    {
        var mock = Substitute.For<IEventApiClient>();

        // Configure default successful empty responses for events
        mock.EventGETAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResultOfEventListDto
            {
                Items = new List<EventListDto>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = 50
            });

        // Configure default successful empty responses for my events
        mock.MyAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResultOfEventListDto
            {
                Items = new List<EventListDto>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = 50
            });

        // Configure default successful empty responses for organizations
        mock.OrganizationGETAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResultOfOrganizationListDto
            {
                Items = new List<OrganizationListDto>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = 50
            });

        // Configure default successful empty responses for my organizations
        mock.My2Async(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResultOfOrganizationListDto
            {
                Items = new List<OrganizationListDto>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = 50
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
        services.AddSingleton(CreateAuthStateService(userId, tenantId));
        services.AddSingleton(CreateUserService());
        services.AddSingleton(CreateCategoryService());
        services.AddSingleton(CreateTagService());
        services.AddSingleton(CreateLocationService());
        services.AddSingleton(CreateImageStorageService());
        RegisterLookupServiceMocks(services);
    }

    #endregion
}
