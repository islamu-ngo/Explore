using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Services;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;

namespace Explore.Blazor.Client.Tests.Common;

/// <summary>
/// Custom BUnit test context with pre-configured services for ISLAMU Event Blazor testing.
/// Provides MudBlazor support, authentication mocking, and common service mock helpers.
/// </summary>
/// <remarks>
/// <para>
/// This context follows enterprise testing best practices:
/// - User IDs are Guid (matching domain model) - converted to string for JWT claims
/// - Tenant IDs are Guid (matching domain model and service interface)
/// - MudBlazor services are pre-registered for proper component rendering
/// - JSInterop is in loose mode to allow unmocked JS calls to pass
/// </para>
/// <para>
/// Usage:
/// <code>
/// using var ctx = new BlazorTestContext();
/// ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
/// ctx.AddMockService&lt;IEventService&gt;(svc => svc.GetAllEventsAsync().Returns(events));
/// var cut = ctx.RenderComponent&lt;EventList&gt;();
/// </code>
/// </para>
/// </remarks>
public class BlazorTestContext : Bunit.TestContext
{
    private readonly TestAuthorizationContext _authContext;

    /// <summary>
    /// Creates a new BlazorTestContext with MudBlazor and authentication support pre-configured.
    /// </summary>
    public BlazorTestContext()
    {
        // Add MudBlazor services for proper component rendering
        // Configure popover service to use testing-friendly mode
        Services.AddMudServices(config =>
        {
            // Configure popover service for testing (no JS interop required)
            config.PopoverOptions.ThrowOnDuplicateProvider = false;
        });

        // Add bUnit's fake authorization (provides CascadingAuthenticationState)
        _authContext = this.AddTestAuthorization();

        // Configure JSInterop to loose mode (allows unmocked JS calls to pass)
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Mock IBrowserViewportService to prevent viewport detection issues in tests.
        // MudBlazor's real service uses JS interop to detect viewport size, which defaults
        // to 0x0 in bUnit (triggering mobile mode). This mock keeps desktop mode in tests.
        var viewportService = Substitute.For<IBrowserViewportService>();
        Services.AddSingleton(viewportService);

        // Setup common MudBlazor JSInterop handlers
        SetupMudBlazorJsInterop();

        // Add common infrastructure services
        Services.AddLogging();

        // Localization services (required by LanguagePicker in NavMenu/MainLayout)
        var translationService = Substitute.For<ITranslationService>();
        translationService.CurrentLanguage.Returns("en");
        translationService.T(Arg.Any<string>(), Arg.Any<string?>()).Returns(ci => ci.ArgAt<string>(0));
        translationService.GetAvailableLanguagesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<string> { "en" });
        translationService.PreloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        Services.AddSingleton(translationService);
        Services.AddSingleton(Substitute.For<IHttpClientFactory>());

        // Notification services (required by NotificationBell in NavMenu/MainLayout)
        Services.AddSingleton(MockServiceFactory.CreateNotificationService());

        var groupService = Substitute.For<IGroupService>();
        groupService.GetMyGroupsAsync().Returns(new List<GroupPublisherListDto>());
        Services.AddSingleton(groupService);
    }

    /// <summary>
    /// Configure JSInterop handlers for MudBlazor components.
    /// MudBlazor uses JS interop for various features - we mock these for testing.
    /// </summary>
    private void SetupMudBlazorJsInterop()
    {
        // MudBlazor resize observer
        JSInterop.SetupVoid("mudResizeObserver.connect");
        JSInterop.SetupVoid("mudResizeObserver.disconnect");

        // MudBlazor event listener
        JSInterop.SetupVoid("mudEventListener.connect");
        JSInterop.SetupVoid("mudEventListener.disconnect");

        // MudBlazor scroll manager
        JSInterop.SetupVoid("mudScrollManager.lockScroll");
        JSInterop.SetupVoid("mudScrollManager.unlockScroll");

        // MudBlazor popover
        JSInterop.SetupVoid("mudPopover.initialize");
        JSInterop.SetupVoid("mudPopover.connect");
        JSInterop.SetupVoid("mudPopover.disconnect");
        JSInterop.Setup<int>("mudPopover.countProviders").SetResult(1);

        // MudBlazor element reference - use object since BoundingClientRect may be internal
        // JSInterop.Mode is Loose, so this mock is optional but helps avoid warnings
        JSInterop.SetupVoid("mudElementRef.getBoundingClientRect");

        // MudBlazor keyboard
        JSInterop.SetupVoid("mudKeyInterceptor.connect");
        JSInterop.SetupVoid("mudKeyInterceptor.disconnect");
    }

    /// <summary>
    /// Renders a component wrapped with MudBlazor providers (MudPopoverProvider, etc.).
    /// Use this for components that use MudPopover, MudMenu, or other popover-based components.
    /// </summary>
    /// <typeparam name="TComponent">Component type to render</typeparam>
    /// <param name="parameterBuilder">Optional parameter builder</param>
    /// <returns>Rendered component</returns>
    public IRenderedComponent<TComponent> RenderMudComponent<TComponent>(
        Action<ComponentParameterCollectionBuilder<TComponent>>? parameterBuilder = null)
        where TComponent : IComponent
    {
        // Render the component within a MudPopoverProvider wrapper
        var wrapper = RenderComponent<MudPopoverProvider>();

        // Now render the actual component
        return parameterBuilder != null
            ? RenderComponent<TComponent>(parameterBuilder)
            : RenderComponent<TComponent>();
    }

    /// <summary>
    /// Configure authenticated user with claims for testing authorized components.
    /// </summary>
    /// <param name="userId">User ID as Guid (domain model type) - stored as string in JWT claims</param>
    /// <param name="name">Display name for the user</param>
    /// <param name="email">Optional email address</param>
    /// <param name="tenantId">Optional tenant ID for multi-tenancy testing</param>
    /// <remarks>
    /// <para>
    /// The userId parameter is Guid because that's the domain model type (User.Id is Guid).
    /// JWT claims store this as a string, so we convert it internally.
    /// This design bridges domain expectations with JWT reality.
    /// </para>
    /// <para>
    /// Claims are added in the same order the application expects:
    /// sub (OIDC standard), nameidentifier (legacy), name, email, tenant_id
    /// </para>
    /// </remarks>
    public void SetAuthenticatedUser(Guid userId, string? name = null, string? email = null, Guid? tenantId = null)
    {
        var userIdString = userId.ToString();

        var claims = new List<Claim>
        {
            // Primary user ID claims - stored as string (JWT standard)
            // Fallback order in AuthStateService: nameidentifier -> sub -> sid
            new("sub", userIdString),
            new(ClaimTypes.NameIdentifier, userIdString)
        };

        if (!string.IsNullOrEmpty(name))
            claims.Add(new Claim(ClaimTypes.Name, name));
        if (!string.IsNullOrEmpty(email))
            claims.Add(new Claim(ClaimTypes.Email, email));
        if (tenantId.HasValue)
            claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));

        _authContext.SetAuthorized(name ?? "TestUser");
        _authContext.SetClaims(claims.ToArray());
    }

    /// <summary>
    /// Configure authenticated user with specific roles.
    /// </summary>
    /// <param name="userId">User ID as Guid</param>
    /// <param name="name">Display name for the user</param>
    /// <param name="roles">Roles to assign to the user</param>
    public void SetAuthenticatedUserWithRoles(Guid userId, string name, params string[] roles)
    {
        SetAuthenticatedUser(userId, name);
        _authContext.SetRoles(roles);
    }

    /// <summary>
    /// Configure authenticated user with specific policies.
    /// </summary>
    /// <param name="userId">User ID as Guid</param>
    /// <param name="name">Display name for the user</param>
    /// <param name="policies">Policies to authorize for the user</param>
    public void SetAuthenticatedUserWithPolicies(Guid userId, string name, params string[] policies)
    {
        SetAuthenticatedUser(userId, name);
        _authContext.SetPolicies(policies);
    }

    /// <summary>
    /// Configure authenticated user with additional custom claims (e.g., admin claims).
    /// </summary>
    /// <param name="userId">User ID as Guid</param>
    /// <param name="name">Display name for the user</param>
    /// <param name="additionalClaims">Additional claims to add beyond standard identity claims</param>
    public void SetAuthenticatedUserWithClaims(Guid userId, string name, params Claim[] additionalClaims)
    {
        var userIdString = userId.ToString();

        var claims = new List<Claim>
        {
            new("sub", userIdString),
            new(ClaimTypes.NameIdentifier, userIdString),
            new(ClaimTypes.Name, name)
        };

        claims.AddRange(additionalClaims);

        _authContext.SetAuthorized(name);
        _authContext.SetClaims(claims.ToArray());
    }

    /// <summary>
    /// Configure anonymous (unauthenticated) user for testing public components.
    /// </summary>
    public void SetAnonymousUser()
    {
        _authContext.SetNotAuthorized();
    }

    /// <summary>
    /// Configure authorizing state (loading authentication).
    /// Useful for testing loading states.
    /// </summary>
    public void SetAuthorizingState()
    {
        _authContext.SetAuthorizing();
    }

    /// <summary>
    /// Register a mock service using NSubstitute.
    /// </summary>
    /// <typeparam name="T">Service interface type</typeparam>
    /// <returns>The mock instance for further configuration</returns>
    public T AddMockService<T>() where T : class
    {
        var mock = Substitute.For<T>();
        Services.AddSingleton(mock);
        return mock;
    }

    /// <summary>
    /// Register a mock service with inline configuration.
    /// </summary>
    /// <typeparam name="T">Service interface type</typeparam>
    /// <param name="configure">Configuration action for setting up mock behavior</param>
    /// <returns>The configured mock instance</returns>
    /// <example>
    /// ctx.AddMockService&lt;IEventService&gt;(svc =>
    ///     svc.GetAllEventsAsync().Returns(eventList));
    /// </example>
    public T AddMockService<T>(Action<T> configure) where T : class
    {
        var mock = Substitute.For<T>();
        configure(mock);
        Services.AddSingleton(mock);
        return mock;
    }

    /// <summary>
    /// Register a concrete service instance.
    /// </summary>
    public void AddService<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        Services.AddSingleton<TService, TImplementation>();
    }

    /// <summary>
    /// Register a scoped mock service (useful for per-request services).
    /// </summary>
    public T AddScopedMockService<T>() where T : class
    {
        var mock = Substitute.For<T>();
        Services.AddScoped(_ => mock);
        return mock;
    }

    /// <summary>
    /// Add TenantConfiguration options for multi-tenancy testing.
    /// </summary>
    /// <param name="tenantId">Default tenant ID</param>
    /// <param name="enabled">Whether multi-tenancy is enabled</param>
    public void AddTenantConfiguration(Guid? tenantId = null, bool enabled = false)
    {
        var config = new TenantConfiguration
        {
            DefaultTenantId = tenantId ?? Guid.Parse("018e4e5c-7f00-7000-8000-000000000001"),
            Enabled = enabled
        };
        Services.AddSingleton(Options.Create(config));
    }

    /// <summary>
    /// Register all core service mocks with default configuration.
    /// </summary>
    /// <param name="userId">Optional user ID for auth service</param>
    /// <param name="tenantId">Optional tenant ID for auth service</param>
    public void AddAllCoreMocks(Guid? userId = null, Guid? tenantId = null)
    {
        MockServiceFactory.RegisterAllCoreMocks(Services, userId, tenantId);
    }

    /// <summary>
    /// Register all lookup service mocks with empty data.
    /// </summary>
    public void AddLookupServiceMocks()
    {
        MockServiceFactory.RegisterLookupServiceMocks(Services);
    }

    /// <summary>
    /// Register all lookup service mocks with pre-generated test data.
    /// </summary>
    /// <param name="itemCount">Number of items to generate for each lookup</param>
    public void AddLookupServiceMocksWithData(int itemCount = 5)
    {
        MockServiceFactory.RegisterLookupServiceMocksWithData(Services, itemCount);
    }
}
