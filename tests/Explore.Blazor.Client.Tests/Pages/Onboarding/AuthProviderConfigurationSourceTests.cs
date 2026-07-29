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
        await Assert.That(source).Contains("Label=\"Admin username (Required)\"");
        await Assert.That(source).Contains("aria-label=\"One-time Keycloak admin username (required)\"");
        await Assert.That(source).Contains("Label=\"Admin password (Required)\"");
        await Assert.That(source).Contains("aria-label=\"One-time Keycloak admin password (required)\"");
        await Assert.That(source).Contains("Label=\"BFF client secret (Required)\"");
        await Assert.That(source).Contains("aria-label=\"Blazor BFF client secret (required)\"");
        await Assert.That(source).Contains("The admin credential is not stored after this request.");
    }

    [Test]
    public async Task AuthProviderConfiguration_ShouldCallBootstrapService_AndClearOneTimeSecrets()
    {
        var source = await ReadAuthProviderConfigurationAsync();

        await Assert.That(source).Contains("BootstrapKeycloakWithSecretCleanupAsync()");
        await Assert.That(source).Contains("finally");
        await Assert.That(source).Contains("ClearKeycloakBootstrapSecrets();");
        await Assert.That(source).Contains("_keycloakBootstrap.BootstrapAdminPassword = string.Empty;");
        await Assert.That(source).Contains("_keycloakBootstrap.BootstrapAdminUsername = string.Empty;");
        await Assert.That(source).Contains("_keycloakBootstrap.BlazorClientSecret = string.Empty;");
        await Assert.That(source).Contains("_keycloakBootstrap.ApiClientSecret = string.Empty;");
        await Assert.That(source).Contains("catch (Exception)");
        await Assert.That(source).DoesNotContain("Logger.LogWarning(ex, \"Failed to save authentication provider configuration.\"");
    }

    [Test]
    public async Task AuthProviderConfiguration_ShouldAllowBootstrap_WhenKeycloakDetectedFromDeployment()
    {
        var source = await ReadAuthProviderConfigurationAsync();

        await Assert.That(source).DoesNotContain("!_model.KeycloakDetectedFromEnvironment &&");
        await Assert.That(source).Contains("You can still patch the external realm during setup before the first admin account exists.");
    }

    [Test]
    public async Task AuthProviderConfiguration_ShouldRequireBootstrapCredentials_WhenKeycloakIsDetectedFromDeployment()
    {
        var source = await ReadAuthProviderConfigurationAsync();

        await Assert.That(source).Contains("_keycloakConfigurationMode == KeycloakConfigurationMode.Manual\n            ? _model.KeycloakDetectedFromEnvironment == true");
        await Assert.That(source).Contains(": HasKeycloakBootstrapCredentials;");
    }

    [Test]
    public async Task AuthProviderConfiguration_ShouldPreserveKeycloakBasePath_WhenSeedingBootstrapDefaults()
    {
        var source = await ReadAuthProviderConfigurationAsync();

        await Assert.That(source).Contains("var basePath = authorityUri.AbsolutePath[..realmIndex].TrimEnd('/');");
        await Assert.That(source).Contains("authorityUri.GetLeftPart(UriPartial.Authority) + basePath");
        await Assert.That(source).DoesNotContain("_keycloakBootstrap.BlazorClientSecret = _model.KeycloakClientSecret;");
        await Assert.That(source).Contains("_model.KeycloakClientSecret = string.Empty;");
        await Assert.That(source).Contains("_model.GoogleClientSecret = string.Empty;");
    }

    [Test]
    public async Task AuthProviderConfiguration_ShouldIsolateFallbackTextDirection()
    {
        var source = await ReadAuthProviderConfigurationAsync();

        await Assert.That(source).Contains("class=\"auth-provider-configuration\" dir=\"auto\"");
        await Assert.That(source).Contains("<MudContainer MaxWidth=\"MaxWidth.Small\">");
    }

    [Test]
    public async Task AuthProviderConfiguration_ShouldUseKeyboardNativeBootstrapModeSelection()
    {
        var source = await ReadAuthProviderConfigurationAsync();

        await Assert.That(source).DoesNotContain("@onclick=\"SelectKeycloakBootstrapMode\"");
        await Assert.That(source).DoesNotContain("style=\"cursor: pointer;\"");
    }

    [Test]
    public async Task AuthProviderConfiguration_ShouldUseSemanticProviderDisclosures()
    {
        var source = await ReadAuthProviderConfigurationAsync();
        var styles = await ReadAuthProviderStylesAsync();

        await Assert.That(source).Contains("<details");
        await Assert.That(source).Contains("<summary");
        await Assert.That(source).Contains("aria-labelledby=\"keycloak-provider-title\"");
        await Assert.That(source).Contains("aria-describedby=\"keycloak-provider-description\"");
        await Assert.That(source).DoesNotContain("MudExpansionPanel");
        await Assert.That(source).DoesNotContain("_isKeycloakPanelExpanded");
        await Assert.That(source).DoesNotContain("Icons.Material.Filled.Info");
        await Assert.That(styles).Contains(".auth-provider-configuration__provider-summary::marker");
        await Assert.That(styles).DoesNotContain("list-style: none");
        await Assert.That(styles).Contains(".auth-provider-configuration__provider-heading {\n        display: inline;");
        await Assert.That(styles).Contains(".auth-provider-configuration__provider-badge {\n        display: inline-block;");
        await Assert.That(source).Contains("class=\"auth-provider-configuration__enable-switch\"");
        await Assert.That(styles).Contains(".auth-provider-configuration__enable-switch:has(input:focus-visible)");
    }

    [Test]
    public async Task AuthProviderConfiguration_ShouldPreserveDarkModeTextContrast()
    {
        var styles = await ReadAuthProviderStylesAsync();

        await Assert.That(styles).Contains("::placeholder");
        await Assert.That(styles).Contains(".mud-input-helper-text");
        await Assert.That(styles).Contains(".mud-alert-message");
        await Assert.That(styles).Contains(".mud-alert-message .mud-typography");
        await Assert.That(styles).Contains("var(--mud-palette-text-primary)");
    }

    [Test]
    public async Task AuthProviderConfiguration_ShouldKeepKeycloakManagementAndUseAuthoritativeAuthorizationHandoff()
    {
        var source = await ReadAuthProviderConfigurationAsync();

        await Assert.That(source).Contains("ShouldSkipAuthorizationProviderStepAsync()");
        await Assert.That(source).Contains("skipAuthorizationProvider ? \"/onboarding/instance\" : \"/onboarding/authz-provider\"");
        await Assert.That(source).Contains("You can still patch the external realm during setup before the first admin account exists.");
    }

    [Test]
    public async Task AuthProviderConfiguration_ShouldMoveFocusIntoExpandedProviderManagement()
    {
        var source = await ReadAuthProviderConfigurationAsync();

        await Assert.That(source).Contains("id=\"keycloak-provider-summary\"");
        await Assert.That(source).Contains("FocusByIdAsync(\"keycloak-provider-summary\")");
        await Assert.That(source).Contains("AnnouncePoliteAsync(\"Authentication provider configuration opened.\")");
    }

    [Test]
    public async Task AuthProviderConfiguration_ShouldPreserveHardReloadAfterSchemeRefresh()
    {
        var source = await ReadAuthProviderConfigurationAsync();

        await Assert.That(source).Contains("RefreshSchemesAsync(CancellationToken.None)");
        await Assert.That(source).Contains("forceLoad: true");
    }

    [Test]
    public async Task AuthProviderConfiguration_ShouldProjectWorkspaceAndConfirmDirtyExitWithoutBrowserStorage()
    {
        var source = await ReadAuthProviderConfigurationAsync();

        await Assert.That(source).Contains("<OnboardingWorkspace");
        await Assert.That(source).Contains("WorkspaceSteps");
        await Assert.That(source).Contains("role=\"alertdialog\"");
        await Assert.That(source).Contains("SaveFocusAsync()");
        await Assert.That(source).Contains("RestoreFocusAsync(\"#onboarding-exit\")");
        await Assert.That(source).DoesNotContain("localStorage");
        await Assert.That(source).DoesNotContain("sessionStorage");
    }

    private static async Task<string> ReadAuthProviderConfigurationAsync()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
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

    private static async Task<string> ReadAuthProviderStylesAsync()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "Explore.Blazor.Client",
                "Pages",
                "Onboarding",
                "AuthProviderConfiguration.razor.css");

            if (File.Exists(candidate))
            {
                return await File.ReadAllTextAsync(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("AuthProviderConfiguration.razor.css was not found.");
    }
}
