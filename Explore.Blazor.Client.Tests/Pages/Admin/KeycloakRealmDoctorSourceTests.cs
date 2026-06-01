// ABOUTME: Source-level tests for the instance-admin Keycloak realm doctor UI.
// ABOUTME: Guards read-only diagnostic affordances and temporary credential cleanup.

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class KeycloakRealmDoctorSourceTests
{
    [Test]
    public async Task InstanceAuthProviderSection_ShouldExpose_ReadOnlyKeycloakDoctor()
    {
        var source = await ReadInstanceAuthProviderSectionAsync();

        await Assert.That(source).Contains("Keycloak realm doctor");
        await Assert.That(source).Contains("Runs read-only diagnostics against the configured realm");
        await Assert.That(source).Contains("Include temporary admin read-only inspection");
        await Assert.That(source).Contains("OnboardingService.RunKeycloakRealmDoctorAsync");
    }

    [Test]
    public async Task InstanceAuthProviderSection_ShouldClear_TemporaryAdminPasswordAfterDoctorRun()
    {
        var source = await ReadInstanceAuthProviderSectionAsync();

        await Assert.That(source).Contains("BootstrapAdminPassword = _useTemporaryKeycloakAdmin ? _temporaryKeycloakAdminPassword : null");
        await Assert.That(source).Contains("_temporaryKeycloakAdminPassword = string.Empty;");
    }

    [Test]
    public async Task InstanceAuthProviderSection_ShouldExpose_ReadOnlyKeycloakSyncPreview()
    {
        var source = await ReadInstanceAuthProviderSectionAsync();

        await Assert.That(source).Contains("Keycloak sync preview");
        await Assert.That(source).Contains("Builds a read-only additive sync plan from the configured realm");
        await Assert.That(source).Contains("Apply is intentionally unavailable until backup-confirmed repair is implemented");
        await Assert.That(source).Contains("OnboardingService.PreviewKeycloakRealmSyncAsync");
    }

    [Test]
    public async Task InstanceAuthProviderSection_ShouldClear_TemporaryAdminPasswordAfterSyncPreview()
    {
        var source = await ReadInstanceAuthProviderSectionAsync();

        await Assert.That(source).Contains("PreviewKeycloakSyncAsync");
        await Assert.That(source).Contains("BootstrapAdminPassword = _useTemporaryKeycloakAdmin ? _temporaryKeycloakAdminPassword : null");
        await Assert.That(source).Contains("_temporaryKeycloakAdminPassword = string.Empty;");
    }

    private static async Task<string> ReadInstanceAuthProviderSectionAsync()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "Explore.Blazor.Client",
                "Pages",
                "Admin",
                "Instance",
                "Components",
                "InstanceAuthProviderSection.razor");

            if (File.Exists(candidate))
                return await File.ReadAllTextAsync(candidate);

            directory = directory.Parent;
        }

        throw new FileNotFoundException("InstanceAuthProviderSection.razor was not found.");
    }
}
