// ABOUTME: Architecture guardrails for the separate Event.ControlPlane.Blazor BFF host.
// ABOUTME: Enforces shared BFF/client usage, shared user secrets, Docker secret posture, and no public-host coupling.

namespace Event.Architecture.Tests;

public sealed class EventControlPlaneBlazorArchitectureTests
{
    private static readonly string RepoRoot = ResolveRepoRoot();
    private static readonly string ControlPlaneBlazorRoot = Path.Combine(RepoRoot, "Event.ControlPlane.Blazor");

    [Test]
    public async Task EventControlPlaneBlazor_Project_MustUseSharedBffClientAndUserSecrets()
    {
        var projectPath = Path.Combine(ControlPlaneBlazorRoot, "Event.ControlPlane.Blazor.csproj");
        await Assert.That(File.Exists(projectPath)).IsTrue()
            .Because("Event.ControlPlane.Blazor must exist as the separate self-hostable control-plane BFF app.");

        var projectXml = await File.ReadAllTextAsync(projectPath);

        await Assert.That(projectXml.Contains("Microsoft.NET.Sdk.Web", StringComparison.Ordinal)).IsTrue()
            .Because("The separate control-plane host must be an ASP.NET Core Blazor/BFF web app.");
        await Assert.That(projectXml.Contains("<UserSecretsId>event-shared-secrets</UserSecretsId>", StringComparison.Ordinal)).IsTrue()
            .Because("The control-plane app must use the same local user-secrets store for Infisical bootstrap credentials.");
        await Assert.That(projectXml.Contains("..\\Event.Web.BffHosting\\Event.Web.BffHosting.csproj", StringComparison.Ordinal)).IsTrue()
            .Because("The separate app must consume the shared BFF hosting security library.");
        await Assert.That(projectXml.Contains("..\\Event.ControlPlane.Client\\Event.ControlPlane.Client.csproj", StringComparison.Ordinal)).IsTrue()
            .Because("The separate app must consume the shared control-plane UI/client library.");
        await Assert.That(projectXml.Contains("..\\Explore.Secrets\\Explore.Secrets.csproj", StringComparison.Ordinal)).IsTrue()
            .Because("The separate app must load runtime secrets through the same Infisical/environment secret library as the public Blazor host.");
        await Assert.That(projectXml.Contains("..\\Explore.Blazor\\Explore.Blazor.csproj", StringComparison.Ordinal)).IsFalse()
            .Because("The separate app must not couple to the existing public Blazor host.");
        await Assert.That(projectXml.Contains("..\\Explore.Blazor.Client\\Explore.Blazor.Client.csproj", StringComparison.Ordinal)).IsFalse()
            .Because("The separate app must not reuse the public Blazor client shell directly.");
        await Assert.That(projectXml.Contains("<PackageReference Include=\"MudBlazor\" />", StringComparison.Ordinal)).IsTrue()
            .Because("The separate host must render shared control-plane primitives that use MudBlazor.");
    }

    [Test]
    public async Task EventControlPlaneBlazor_Program_MustUseControlPlaneBffProfileAndSecretLoading()
    {
        var programPath = Path.Combine(ControlPlaneBlazorRoot, "Program.cs");
        await Assert.That(File.Exists(programPath)).IsTrue()
            .Because("The separate host must have an explicit composition root.");

        var source = await File.ReadAllTextAsync(programPath);
        var requiredTokens = new[]
        {
            "AddInfisicalControlPlaneCompatibility",
            "AddSecretManagement",
            "EventBffHostProfile.ControlPlane",
            "AddEventBffKeycloakAuthentication",
            "AddEventApiProxy",
            "ControlPlaneBffCookieSessionHandler",
            "IEventBffCookieSessionHandler",
            "AddMudServices",
            "AddEventControlPlaneClient",
            "MapEventBffAuthEndpoints",
            "RequireAuthorization(EventBffAuthorizationPolicies.ControlPlaneAccess)"
        };

        var missing = requiredTokens
            .Where(token => !source.Contains(token, StringComparison.Ordinal))
            .ToArray();

        await Assert.That(missing).IsEmpty()
            .Because($"Program.cs must wire the shared BFF/control-plane/secret-loading path. Missing: {string.Join(", ", missing)}");
    }

    [Test]
    public async Task EventControlPlaneBlazor_MudBlazorHostSetup_MustSupportSharedPrimitives()
    {
        var appPath = Path.Combine(ControlPlaneBlazorRoot, "Components", "App.razor");
        var importsPath = Path.Combine(ControlPlaneBlazorRoot, "Components", "_Imports.razor");
        var layoutPath = Path.Combine(ControlPlaneBlazorRoot, "Components", "Layout", "ControlPlaneLayout.razor");

        var appSource = await File.ReadAllTextAsync(appPath);
        var importsSource = await File.ReadAllTextAsync(importsPath);
        var layoutSource = await File.ReadAllTextAsync(layoutPath);

        var requiredTokens = new[]
        {
            "_content/MudBlazor/MudBlazor.min.css",
            "_content/MudBlazor/MudBlazor.min.js",
            "@using MudBlazor",
            "<MudThemeProvider",
            "<MudPopoverProvider",
            "<MudDialogProvider",
            "<MudSnackbarProvider"
        };

        var combined = string.Join('\n', appSource, importsSource, layoutSource);
        var missing = requiredTokens
            .Where(token => !combined.Contains(token, StringComparison.Ordinal))
            .ToArray();

        await Assert.That(missing).IsEmpty()
            .Because($"The separate control-plane host must provide MudBlazor assets and providers for shared RCL primitives. Missing: {string.Join(", ", missing)}");
    }

    [Test]
    public async Task EventControlPlaneBlazor_RenderMode_MustBeInteractiveServerOnly()
    {
        var projectPath = Path.Combine(ControlPlaneBlazorRoot, "Event.ControlPlane.Blazor.csproj");
        var programPath = Path.Combine(ControlPlaneBlazorRoot, "Program.cs");
        var appPath = Path.Combine(ControlPlaneBlazorRoot, "Components", "App.razor");
        var clientRoot = Path.Combine(RepoRoot, "Event.ControlPlane.Client");

        await Assert.That(File.Exists(programPath)).IsTrue()
            .Because("The control-plane BFF host must explicitly configure its component render mode.");
        await Assert.That(File.Exists(appPath)).IsTrue()
            .Because("The control-plane root document must explicitly apply the host render mode.");
        await Assert.That(Directory.Exists(clientRoot)).IsTrue()
            .Because("The shared control-plane client library must exist and stay host neutral.");

        var projectXml = await File.ReadAllTextAsync(projectPath);
        var programSource = await File.ReadAllTextAsync(programPath);
        var appSource = await File.ReadAllTextAsync(appPath);

        await Assert.That(programSource.Contains("AddInteractiveServerComponents()", StringComparison.Ordinal)).IsTrue()
            .Because("The separate control-plane app must register only Interactive Server component services.");
        await Assert.That(programSource.Contains("AddInteractiveServerRenderMode()", StringComparison.Ordinal)).IsTrue()
            .Because("The separate control-plane app must map only the Interactive Server render mode.");
        await Assert.That(appSource.Contains("<HeadOutlet @rendermode=\"InteractiveServer\"", StringComparison.Ordinal)).IsTrue()
            .Because("The control-plane root document must keep head updates on the server circuit.");
        await Assert.That(appSource.Contains("<Routes @rendermode=\"InteractiveServer\"", StringComparison.Ordinal)).IsTrue()
            .Because("The control-plane route tree must run as Interactive Server, not Auto or WebAssembly.");

        var hostForbiddenTokens = new[]
        {
            "AddInteractiveWebAssemblyComponents",
            "AddInteractiveWebAssemblyRenderMode",
            "InteractiveAuto",
            "InteractiveAutoRenderMode",
            "InteractiveWebAssembly",
            "InteractiveWebAssemblyRenderMode",
            "WebAssemblyHost"
        };

        var hostViolations = new List<string>();
        foreach (var file in EnumerateSourceFiles(ControlPlaneBlazorRoot))
        {
            var content = await File.ReadAllTextAsync(file);
            var relative = Path.GetRelativePath(RepoRoot, file).Replace('\\', '/');
            foreach (var token in hostForbiddenTokens)
            {
                if (content.Contains(token, StringComparison.Ordinal))
                {
                    hostViolations.Add($"{relative} contains forbidden render-mode token '{token}'");
                }
            }
        }

        foreach (var token in hostForbiddenTokens)
        {
            if (projectXml.Contains(token, StringComparison.Ordinal))
            {
                hostViolations.Add($"Event.ControlPlane.Blazor/Event.ControlPlane.Blazor.csproj contains forbidden render-mode token '{token}'");
            }
        }

        await Assert.That(hostViolations).IsEmpty()
            .Because(string.Join('\n', hostViolations));

        var clientForbiddenTokens = new[]
        {
            "@rendermode",
            "IComponentRenderMode",
            "InteractiveServer",
            "InteractiveAuto",
            "InteractiveWebAssembly",
            "RenderMode"
        };

        var clientViolations = new List<string>();
        foreach (var file in EnumerateSourceFiles(clientRoot))
        {
            var content = await File.ReadAllTextAsync(file);
            var relative = Path.GetRelativePath(RepoRoot, file).Replace('\\', '/');
            foreach (var token in clientForbiddenTokens)
            {
                if (content.Contains(token, StringComparison.Ordinal))
                {
                    clientViolations.Add($"{relative} contains forbidden host render-mode token '{token}'");
                }
            }
        }

        await Assert.That(clientViolations).IsEmpty()
            .Because(string.Join('\n', clientViolations));
    }

    [Test]
    public async Task EventControlPlaneBlazor_Source_MustNotDependOnPublicBlazorShellOrBrowserTokenStorage()
    {
        var forbiddenTokens = new[]
        {
            "Explore.Blazor.Client",
            "Explore.Blazor",
            "localStorage",
            "sessionStorage",
            "ProtectedLocalStorage",
            "AccessToken",
            "RefreshToken"
        };

        var violations = new List<string>();
        foreach (var file in EnumerateSourceFiles(ControlPlaneBlazorRoot))
        {
            var content = await File.ReadAllTextAsync(file);
            var relative = Path.GetRelativePath(RepoRoot, file).Replace('\\', '/');
            foreach (var token in forbiddenTokens)
            {
                if (content.Contains(token, StringComparison.Ordinal))
                {
                    violations.Add($"{relative} contains forbidden token '{token}'");
                }
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because(string.Join('\n', violations));
    }

    [Test]
    public async Task EventControlPlaneBlazor_Routes_MustUseProtectedControlPlaneShellOnly()
    {
        var routesPath = Path.Combine(ControlPlaneBlazorRoot, "Components", "Routes.razor");
        var programPath = Path.Combine(ControlPlaneBlazorRoot, "Program.cs");

        var routesSource = await File.ReadAllTextAsync(routesPath);
        var programSource = await File.ReadAllTextAsync(programPath);

        var requiredRouteTokens = new[]
        {
            "AuthorizeRouteView",
            "DefaultLayout=\"@typeof(ControlPlaneLayout)\"",
            "<RedirectToLogin />",
            "ControlPlaneClientAssembly.Value"
        };

        var missingRouteTokens = requiredRouteTokens
            .Where(token => !routesSource.Contains(token, StringComparison.Ordinal))
            .ToArray();

        await Assert.That(missingRouteTokens).IsEmpty()
            .Because($"The separate host route tree must use the protected control-plane shell only. Missing: {string.Join(", ", missingRouteTokens)}");

        await Assert.That(routesSource.Contains("MainLayout", StringComparison.Ordinal)).IsFalse()
            .Because("The separate host must not render through the public/tenant Blazor shell.");
        await Assert.That(programSource.Contains("MapRazorComponents<App>()", StringComparison.Ordinal)).IsTrue();
        await Assert.That(programSource.Contains("MapReverseProxy()", StringComparison.Ordinal)).IsTrue();
        await Assert.That(CountOccurrences(programSource, "RequireAuthorization(EventBffAuthorizationPolicies.ControlPlaneAccess)")).IsEqualTo(2)
            .Because("Both the control-plane UI endpoint and API proxy must require the coarse instance-admin BFF policy.");
    }

    [Test]
    public async Task EventControlPlaneBlazor_Dockerfile_MustSupportRuntimeEnvAndInfisicalSecrets()
    {
        var dockerfilePath = Path.Combine(ControlPlaneBlazorRoot, "Dockerfile");
        await Assert.That(File.Exists(dockerfilePath)).IsTrue()
            .Because("The self-hostable control-plane app must ship with its own Dockerfile.");

        var source = await File.ReadAllTextAsync(dockerfilePath);
        var requiredTokens = new[]
        {
            "Event.ControlPlane.Blazor.csproj",
            "Event.ControlPlane.Client.csproj",
            "Event.Web.BffHosting.csproj",
            "Explore.Secrets.csproj",
            "Bff__Authentication__ClientSecret",
            "SecretProvider__Provider=Infisical",
            "Infisical__ProjectId",
            "Infisical__ClientId",
            "Infisical__ClientSecret",
            "ENTRYPOINT [\"dotnet\", \"Event.ControlPlane.Blazor.dll\"]"
        };

        var missing = requiredTokens
            .Where(token => !source.Contains(token, StringComparison.Ordinal))
            .ToArray();

        await Assert.That(missing).IsEmpty()
            .Because($"Dockerfile must describe runtime env-var and Infisical secret configuration. Missing: {string.Join(", ", missing)}");
        await Assert.That(source.Contains("ENV Bff__Authentication__ClientSecret=", StringComparison.Ordinal)).IsFalse()
            .Because("The image must not bake control-plane client secrets into Docker layers.");
        await Assert.That(source.Contains("ENV Infisical__ClientSecret=", StringComparison.Ordinal)).IsFalse()
            .Because("The image must not bake Infisical bootstrap secrets into Docker layers.");
    }

    [Test]
    public async Task KeycloakSeedFiles_MustDefineAndSyncControlPlaneClient()
    {
        var realmExportPath = Path.Combine(RepoRoot, "docker", "keycloak", "realm-export.json");
        var testRealmPath = Path.Combine(RepoRoot, "docker", "keycloak", "ISLAMU-realm.test.json");
        var initScriptPath = Path.Combine(RepoRoot, "docker", "keycloak", "keycloak-init.sh");

        var realmExport = await File.ReadAllTextAsync(realmExportPath);
        var testRealm = await File.ReadAllTextAsync(testRealmPath);
        var initScript = await File.ReadAllTextAsync(initScriptPath);

        await Assert.That(realmExport.Contains("\"clientId\": \"islamu-event-control-plane\"", StringComparison.Ordinal)).IsTrue()
            .Because("The Docker Keycloak realm export must seed the dedicated control-plane confidential client.");
        await Assert.That(realmExport.Contains("\"secret\": \"islamu-event-control-plane-secret\"", StringComparison.Ordinal)).IsTrue()
            .Because("The local realm export keeps a disposable default that keycloak-init can overwrite from deployment secrets.");
        await Assert.That(realmExport.Contains("\"included.client.audience\": \"islamu-event-api\"", StringComparison.Ordinal)).IsTrue()
            .Because("The control-plane client access token must receive the API audience for BFF token forwarding.");
        await Assert.That(realmExport.Contains("\"smtpServer\"", StringComparison.Ordinal)).IsFalse()
            .Because("The deployable realm export must not bake local SMTP infrastructure into self-hosted deployments.");

        await Assert.That(testRealm.Contains("\"clientId\": \"islamu-event-control-plane\"", StringComparison.Ordinal)).IsTrue()
            .Because("The integration-test realm must mirror the dedicated control-plane client.");
        await Assert.That(testRealm.Contains("\"secret\": \"test-control-plane-secret\"", StringComparison.Ordinal)).IsTrue()
            .Because("Tests need a deterministic non-production control-plane client secret.");
        await Assert.That(testRealm.Contains("\"smtpServer\"", StringComparison.Ordinal)).IsFalse()
            .Because("The test realm export should stay portable; SMTP is applied by environment-driven bootstrap.");

        await Assert.That(initScript.Contains("KEYCLOAK_CONTROL_PLANE_CLIENT_ID", StringComparison.Ordinal)).IsTrue();
        await Assert.That(initScript.Contains("KEYCLOAK_CONTROL_PLANE_CLIENT_SECRET", StringComparison.Ordinal)).IsTrue()
            .Because("keycloak-init must be able to synchronize the dedicated control-plane client secret.");
        await Assert.That(initScript.Contains("set_client_secret \"$KEYCLOAK_CONTROL_PLANE_CLIENT_ID\"", StringComparison.Ordinal)).IsTrue()
            .Because("The control-plane client secret must be updated by client id rather than manual Keycloak UI edits.");
        await Assert.That(initScript.Contains("KEYCLOAK_SMTP_HOST", StringComparison.Ordinal)).IsTrue()
            .Because("keycloak-init must accept environment-driven SMTP settings instead of relying on realm-export constants.");
        await Assert.That(initScript.Contains("sync_realm_smtp_settings", StringComparison.Ordinal)).IsTrue()
            .Because("Persistent realms must receive optional SMTP settings from environment variables without dashboard edits.");
    }

    [Test]
    public async Task EventControlPlaneBlazor_DeploymentFiles_MustExposeSelfHostableService()
    {
        var composePath = Path.Combine(RepoRoot, "docker-compose.yml");
        var envExamplePath = Path.Combine(RepoRoot, ".env.example");
        var appHostPath = Path.Combine(RepoRoot, "Explore.AppHost", "AppHost.cs");
        var appHostProjectPath = Path.Combine(RepoRoot, "Explore.AppHost", "Explore.AppHost.csproj");

        var compose = await File.ReadAllTextAsync(composePath);
        var envExample = await File.ReadAllTextAsync(envExamplePath);
        var appHost = await File.ReadAllTextAsync(appHostPath);
        var appHostProject = await File.ReadAllTextAsync(appHostProjectPath);

        var composeRequiredTokens = new[]
        {
            "islamu-event-control-plane:",
            "profiles: [\"control-plane\"]",
            "Event.ControlPlane.Blazor/Dockerfile",
            "KEYCLOAK_CONTROL_PLANE_CLIENT_ID",
            "KEYCLOAK_CONTROL_PLANE_CLIENT_SECRET",
            "KEYCLOAK_SMTP_HOST",
            "Bff__Authentication__MetadataAddress",
            "CONTROL_PLANE_API_ENDPOINT",
            "CONTROL_PLANE_HTTP_PORT",
            "condition: service_completed_successfully"
        };
        var missingComposeTokens = composeRequiredTokens
            .Where(token => !compose.Contains(token, StringComparison.Ordinal))
            .ToArray();

        await Assert.That(missingComposeTokens).IsEmpty()
            .Because($"docker-compose.yml must expose the optional self-hostable control-plane profile. Missing: {string.Join(", ", missingComposeTokens)}");

        var envRequiredTokens = new[]
        {
            "CONTROL_PLANE_HTTP_PORT",
            "KEYCLOAK_CONTROL_PLANE_CLIENT_ID",
            "KEYCLOAK_CONTROL_PLANE_CLIENT_SECRET",
            "KEYCLOAK_SMTP_HOST",
            "KEYCLOAK_SMTP_PORT",
            "KEYCLOAK_SMTP_FROM",
            "CONTROL_PLANE_API_ENDPOINT"
        };
        var missingEnvTokens = envRequiredTokens
            .Where(token => !envExample.Contains(token, StringComparison.Ordinal))
            .ToArray();

        await Assert.That(missingEnvTokens).IsEmpty()
            .Because($".env.example must document the control-plane Compose and secret inputs. Missing: {string.Join(", ", missingEnvTokens)}");

        await Assert.That(appHostProject.Contains("..\\Event.ControlPlane.Blazor\\Event.ControlPlane.Blazor.csproj", StringComparison.Ordinal)).IsTrue()
            .Because("Aspire AppHost must reference the separate control-plane BFF project.");
        await Assert.That(appHost.Contains("Projects.Event_ControlPlane_Blazor", StringComparison.Ordinal)).IsTrue()
            .Because("Aspire AppHost must register the generated control-plane project resource.");
        await Assert.That(appHost.Contains("\"event-control-plane\"", StringComparison.Ordinal)).IsTrue()
            .Because("Aspire AppHost must expose a stable control-plane resource name.");
        await Assert.That(appHost.Contains("WithReference(exploreAPI)", StringComparison.Ordinal)).IsTrue()
            .Because("The control-plane BFF should resolve the API through Aspire service discovery instead of hardcoded ports.");
        await Assert.That(appHost.Contains("ConfigureFullLocalControlPlane", StringComparison.Ordinal)).IsTrue()
            .Because("Full-local Aspire must inject local Keycloak settings for the dedicated control-plane client.");
        await Assert.That(appHost.Contains("KEYCLOAK_CONTROL_PLANE_CLIENT_SECRET", StringComparison.Ordinal)).IsTrue()
            .Because("Full-local Aspire must provide a deterministic disposable control-plane client secret.");
        await Assert.That(appHost.Contains("\"keycloak-init\"", StringComparison.Ordinal)).IsTrue()
            .Because("Full-local Aspire must run the Keycloak bootstrap script for persistent local realms.");
        await Assert.That(appHost.Contains("KEYCLOAK_SMTP_HOST", StringComparison.Ordinal)).IsTrue()
            .Because("Full-local Aspire must configure Keycloak SMTP through Mailpit without dashboard edits.");
        await Assert.That(appHost.Contains("WaitForCompletion(resources.KeycloakInit)", StringComparison.Ordinal)).IsTrue()
            .Because("Apps must wait until Keycloak realm/client/SMTP bootstrap has completed.");
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root) =>
        Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            .Where(file => !IsGeneratedOrBuildOutput(file));

    private static bool IsGeneratedOrBuildOutput(string file)
    {
        var normalized = file.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.Ordinal)
            || normalized.Contains("/obj/", StringComparison.Ordinal)
            || normalized.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static int CountOccurrences(string source, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }

    private static string ResolveRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Explore.sln"))
                && Directory.Exists(Path.Combine(current.FullName, "Event.ControlPlane.Blazor")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing Explore.sln and Event.ControlPlane.Blazor.");
    }
}
