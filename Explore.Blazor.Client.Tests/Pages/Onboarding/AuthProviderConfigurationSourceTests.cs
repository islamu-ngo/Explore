// ABOUTME: Source-level tests for auth-provider onboarding UI bootstrap affordances.
// ABOUTME: Guards one-time Keycloak credential handling until full bUnit interaction coverage is added.

namespace Explore.Blazor.Client.Tests.Pages.Onboarding;

public class AuthProviderConfigurationSourceTests
{
    [Test]
    public async Task AuthProviderConfiguration_ShouldExpose_KeycloakBootstrapMode()
    {
        var source = await ReadAuthProviderConfigurationAsync();

        await Assert.That(source).Contains("Use an already configured Keycloak realm");
        await Assert.That(source).Contains("Let ISLAMU configure Keycloak clients now");
        await Assert.That(source).Contains("One-time Keycloak admin username (Required)");
        await Assert.That(source).Contains("One-time Keycloak admin password (Required)");
        await Assert.That(source).Contains("The admin credential is not stored after this request.");
    }

    [Test]
    public async Task AuthProviderConfiguration_ShouldCallBootstrapService_AndClearOneTimeSecrets()
    {
        var source = await ReadAuthProviderConfigurationAsync();

        await Assert.That(source).Contains("InstanceOnboardingService.BootstrapKeycloakRealmAsync(_keycloakBootstrap)");
        await Assert.That(source).Contains("ClearKeycloakBootstrapSecrets();");
        await Assert.That(source).Contains("_keycloakBootstrap.BootstrapAdminPassword = string.Empty;");
        await Assert.That(source).Contains("_keycloakBootstrap.BlazorClientSecret = string.Empty;");
    }

    private static async Task<string> ReadAuthProviderConfigurationAsync()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "Explore.Blazor.Client",
                "Pages",
                "Onboarding",
                "AuthProviderConfiguration.razor");

            if (File.Exists(candidate))
            {
                return await File.ReadAllTextAsync(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("AuthProviderConfiguration.razor was not found.");
    }
}
