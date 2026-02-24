// ABOUTME: Component tests for Setup page setup-secret restoration and validation behavior.
// ABOUTME: Verifies stale session secrets are rejected and valid secrets allow onboarding continuation.

using Explore.Blazor.Client.Pages;

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
        var cut = _ctx.RenderMudComponent<Setup>();

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
        var cut = _ctx.RenderMudComponent<Setup>();

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

    private sealed class OkHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
