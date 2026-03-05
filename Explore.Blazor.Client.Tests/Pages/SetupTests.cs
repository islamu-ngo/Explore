// ABOUTME: Component tests for Setup page setup-secret restoration and validation behavior.
// ABOUTME: Verifies stale session secrets are rejected and valid secrets allow onboarding continuation.

using System.Net;
using System.Net.Http.Json;
using Bunit.TestDoubles;
using Explore.Blazor.Client.Pages;
using Explore.Blazor.Client.Pages.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

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

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(new OkHttpHandler())
        {
            BaseAddress = new Uri("https://localhost/")
        });
        _ctx.Services.AddSingleton(httpClientFactory);
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    private static Type GetPageComponentType(string componentName)
    {
        var componentType = typeof(EventList).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == componentName && typeof(IComponent).IsAssignableFrom(t));

        return componentType ?? throw new InvalidOperationException($"Could not find component type '{componentName}'.");
    }

    [Test]
    public async Task RestoreSecretFromSession_WhenStoredSecretIsInvalid_ClearsStoredStateAndShowsError()
    {
        // Arrange
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsCompleted = false,
            IsAuthenticated = true
        });
        _instanceOnboardingService.ValidateSecretAsync("stale-secret").Returns(new SetupSecretValidationResult
        {
            Valid = false,
            Error = "Invalid setup secret."
        });

        _ctx.JSInterop.Setup<string?>("sessionStorage.getItem", "setup-secret").SetResult("stale-secret");

        // Act
        var cut = _ctx.RenderComponent<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("Setup")));

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Stored setup secret is no longer valid. Please enter the current setup secret.", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected stale setup secret warning was not rendered.");
            }
        });

        await _instanceOnboardingService.Received(1).ValidateSecretAsync("stale-secret");
    }

    [Test]
    public async Task RestoreSecretFromSession_WhenStoredSecretIsValid_KeepsValidatedSessionState()
    {
        // Arrange
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsCompleted = false,
            IsAuthenticated = true
        });
        _instanceOnboardingService.ValidateSecretAsync("valid-secret").Returns(new SetupSecretValidationResult
        {
            Valid = true
        });

        _ctx.JSInterop.Setup<string?>("sessionStorage.getItem", "setup-secret").SetResult("valid-secret");

        // Act
        var cut = _ctx.RenderComponent<DynamicComponent>(parameters =>
            parameters.Add(x => x.Type, GetPageComponentType("Setup")));

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Setup secret is validated and your session is authenticated.", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected validated setup secret state was not rendered.");
            }
        });

        await _instanceOnboardingService.Received(1).ValidateSecretAsync("valid-secret");
    }

    [Test]
    public async Task RestoreSecretFromSession_WhenProvidersDetected_ShowsQuickActionsAndConfigureLast()
    {
        // Arrange
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsCompleted = false,
            IsAuthenticated = false
        });
        _instanceOnboardingService.ValidateSecretAsync("valid-secret").Returns(new SetupSecretValidationResult
        {
            Valid = true
        });

        _ctx.JSInterop.Setup<string?>("sessionStorage.getItem", "setup-secret").SetResult("valid-secret");

        // Act
        var cut = _ctx.RenderComponent<DynamicComponent>(parameters =>
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
        _instanceOnboardingService.ValidateSecretAsync("valid-secret").Returns(new SetupSecretValidationResult
        {
            Valid = true
        });

        _ctx.JSInterop.Setup<string?>("sessionStorage.getItem", "setup-secret").SetResult("valid-secret");

        var nav = _ctx.Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("/setup");

        // Act
        var cut = _ctx.RenderComponent<DynamicComponent>(parameters =>
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
        await Assert.That(nav.Uri).EndsWith("/login?provider=keycloak&returnUrl=/setup");
    }

    private sealed class OkHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsolutePath.Contains("/auth/providers", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new
                    {
                        providers = new[]
                        {
                            new { name = "Keycloak", displayName = "Keycloak", type = "button", recommended = true },
                            new { name = "Google", displayName = "Google", type = "button", recommended = false },
                            new { name = "Atproto", displayName = "AT Protocol", type = "handle_input", recommended = false }
                        }
                    })
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
