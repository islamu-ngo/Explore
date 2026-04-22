// ABOUTME: Component tests for Setup page setup-secret restoration and validation behavior.
// ABOUTME: Verifies status display, secret input, provider quick actions, and BFF JS interop integration.

using Explore.Blazor.Client.Models.Responses;
using Explore.Blazor.Client.Pages.Events;

namespace Explore.Blazor.Client.Tests.Pages;

public class SetupTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IInstanceOnboardingService _instanceOnboardingService;

    public SetupTests()
    {
        _ctx = new BlazorTestContext();
        _instanceOnboardingService = Substitute.For<IInstanceOnboardingService>();
        _ctx.Services.AddSingleton(_instanceOnboardingService);
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    /// <summary>
    /// Sets up JS interop for the /js/bff.js module used by Setup.razor.
    /// All BFF calls go through browser fetch via JS interop.
    /// </summary>
    private void SetupBffJsModule(
        bool hasPersistedSecret = false,
        bool isValid = false,
        string? error = null,
        bool persistOk = true,
        bool syncOk = true,
        bool includeProviders = true)
    {
        var module = _ctx.JSInterop.SetupModule("/js/bff.js");

        module.Setup<SetupSecretStatusResponse>("getSetupSecretStatus")
            .SetResult(new SetupSecretStatusResponse
            {
                HasPersistedSecret = hasPersistedSecret,
                IsValid = isValid,
                Error = error
            });

        module.Setup<BffMutationResult>("persistSetupSecret", _ => true)
            .SetResult(new BffMutationResult
            {
                Ok = persistOk,
                Status = persistOk ? 200 : 400,
                Error = persistOk ? null : "Persist failed."
            });

        module.Setup<BffMutationResult>("syncSetupSecret", _ => true)
            .SetResult(new BffMutationResult
            {
                Ok = syncOk,
                Status = syncOk ? 200 : 400,
                Error = syncOk ? null : "Sync failed."
            });

        module.Setup<BffMutationResult>("deleteSetupSecret")
            .SetResult(new BffMutationResult { Ok = true, Status = 200 });

        if (includeProviders)
        {
            module.Setup<AuthProvidersResponse>("fetchJson", invocation =>
                    invocation.Arguments.Count > 0 &&
                    invocation.Arguments[0]?.ToString()?.Contains("/auth/providers") == true)
                .SetResult(new AuthProvidersResponse
                {
                    Providers =
                    [
                        new() { Name = "Keycloak", DisplayName = "Keycloak", Type = "button" },
                        new() { Name = "Google", DisplayName = "Google", Type = "button" },
                        new() { Name = "Atproto", DisplayName = "AT Protocol", Type = "handle_input" }
                    ]
                });
        }
    }

    private static Type GetPageComponentType(string componentName)
    {
        var componentType = typeof(EventList).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == componentName && typeof(IComponent).IsAssignableFrom(t));

        return componentType ?? throw new InvalidOperationException($"Could not find component type '{componentName}'.");
    }

    [Test]
    public async Task RestorePersistedSecret_WhenStoredSecretIsInvalid_ShowsError()
    {
        // Arrange
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsCompleted = false,
            IsAuthenticated = true
        });
        SetupBffJsModule(hasPersistedSecret: false, isValid: false, error: "Invalid setup secret.");

        // Act
        var cut = _ctx.Render<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("Setup")));

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Invalid setup secret.", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected stale setup secret warning was not rendered.");
            }
        });
    }

    [Test]
    public async Task RestorePersistedSecret_WhenStoredSecretIsValid_KeepsValidatedSessionState()
    {
        // Arrange
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsCompleted = false,
            IsAuthenticated = true
        });
        SetupBffJsModule(hasPersistedSecret: true, isValid: true);

        // Act
        var cut = _ctx.Render<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("Setup")));

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Setup secret is validated and your session is authenticated.", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected validated setup secret state was not rendered.");
            }
        });
    }

    [Test]
    public async Task RestorePersistedSecret_WhenProvidersDetected_ShowsQuickActionsAndConfigureLast()
    {
        // Arrange
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsCompleted = false,
            IsAuthenticated = false
        });
        SetupBffJsModule(hasPersistedSecret: true, isValid: true);

        // Act
        var cut = _ctx.Render<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("Setup")));

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Continue with Keycloak", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Continue with Google", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Configure Authentication", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected setup quick action buttons were not rendered.");
            }

            var keycloakIndex = cut.Markup.IndexOf("Continue with Keycloak", StringComparison.OrdinalIgnoreCase);
            var googleIndex = cut.Markup.IndexOf("Continue with Google", StringComparison.OrdinalIgnoreCase);
            var configureIndex = cut.Markup.IndexOf("Configure Authentication", StringComparison.OrdinalIgnoreCase);

            if (configureIndex < keycloakIndex || configureIndex < googleIndex)
            {
                throw new InvalidOperationException("Configure Authentication button should be rendered after provider quick actions.");
            }
        });
    }

    [Test]
    public async Task Setup_WhenKeycloakQuickActionClicked_NavigatesToLoginWithProvider()
    {
        // Arrange
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsCompleted = false,
            IsAuthenticated = false
        });
        SetupBffJsModule(hasPersistedSecret: true, isValid: true);

        var nav = _ctx.Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>();
        nav.NavigateTo("/setup");

        // Act
        var cut = _ctx.Render<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("Setup")));

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Continue with Keycloak", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected Keycloak quick action button was not rendered.");
            }
        });

        var keycloakButton = cut
            .FindAll("button")
            .First(button => button.TextContent.Contains("Continue with Keycloak", StringComparison.OrdinalIgnoreCase));

        keycloakButton.Click();

        // Assert
        await Assert.That(nav.Uri).EndsWith("/login?provider=keycloak&returnUrl=/onboarding/authz-provider");
    }

    [Test]
    public async Task Setup_WhenTimedOut_ShowsExpiredMessage()
    {
        // Arrange
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsCompleted = false,
            IsAuthenticated = false,
            SetupTimedOut = true,
            InstanceStartedAt = DateTime.UtcNow.AddMinutes(-70)
        });
        SetupBffJsModule();

        // Act
        var cut = _ctx.Render<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("Setup")));

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Setup window expired", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected setup timeout message was not rendered.");
            }
        });
    }

    [Test]
    public async Task Setup_WhenCompleted_RedirectsToHome()
    {
        // Arrange
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsCompleted = true,
            IsAuthenticated = true
        });
        SetupBffJsModule();

        var nav = _ctx.Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>();

        // Act
        var cut = _ctx.Render<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("Setup")));

        // Assert
        await Assert.That(nav.Uri).EndsWith("/");
    }
}
