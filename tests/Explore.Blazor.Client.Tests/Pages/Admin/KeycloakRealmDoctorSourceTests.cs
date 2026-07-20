// ABOUTME: Source-level tests for the instance-admin Keycloak realm doctor UI.
// ABOUTME: Guards read-only diagnostic affordances and temporary credential cleanup.

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class KeycloakRealmDoctorSourceTests
{
    [Test]
    public async Task InstanceAuthProviderSection_ShouldDescribeAtprotoLoginWithoutLegacyDecentralizationCopy()
    {
        var source = await ReadInstanceAuthProviderSectionAsync();

        await Assert.That(source).Contains(
            "Users authenticate with linked AT Protocol identities through the server-side OAuth flow.");
        await Assert.That(source).DoesNotContain("decentralization", StringComparison.OrdinalIgnoreCase);
    }

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
        await Assert.That(source).Contains("Preview is read-only");
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

    [Test]
    public async Task InstanceAuthProviderSection_ShouldExpose_BackupConfirmedKeycloakSyncApply()
    {
        var source = await ReadInstanceAuthProviderSectionAsync();

        await Assert.That(source).Contains("Apply Additive Repairs");
        await Assert.That(source).Contains("I have a current Keycloak backup");
        await Assert.That(source).Contains("OnboardingService.ApplyKeycloakRealmSyncAsync");
        await Assert.That(source).Contains("BackupConfirmed = _keycloakBackupConfirmed");
    }

    [Test]
    public async Task InstanceAuthProviderSection_ShouldClear_TemporaryAdminPasswordAfterSyncApply()
    {
        var source = await ReadInstanceAuthProviderSectionAsync();

        await Assert.That(source).Contains("ApplyKeycloakSyncAsync");
        await Assert.That(source).Contains("_temporaryKeycloakAdminPassword = string.Empty;");
        await Assert.That(source).Contains("_keycloakBackupConfirmed = false;");
    }

    [Test]
    public async Task InstanceAuthProviderSection_ShouldExpose_KeycloakClientSecretRotation()
    {
        var source = await ReadInstanceAuthProviderSectionAsync();

        await Assert.That(source).Contains("Keycloak client-secret rotation");
        await Assert.That(source).Contains("Rotate Client Secret");
        await Assert.That(source).Contains("OnboardingService.RotateKeycloakClientSecretAsync");
        await Assert.That(source).Contains("SecretOwnershipMode = Model.KeycloakClientSecretOwnership?.Mode");
    }

    [Test]
    public async Task InstanceAuthProviderSection_ShouldClear_KeycloakRotationSecretsAfterSubmit()
    {
        var source = await ReadInstanceAuthProviderSectionAsync();

        await Assert.That(source).Contains("RotateKeycloakClientSecretAsync");
        await Assert.That(source).Contains("_newKeycloakClientSecret = string.Empty;");
        await Assert.That(source).Contains("_rotationKeycloakAdminPassword = string.Empty;");
        await Assert.That(source).Contains("_confirmApplicationManagedKeycloakSecret = false;");
    }

    private static async Task<string> ReadInstanceAuthProviderSectionAsync()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
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
