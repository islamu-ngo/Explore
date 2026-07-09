// ABOUTME: Architecture guardrails for the shared Event.ControlPlane.Client Razor class library.
// ABOUTME: Prevents host, API, business-layer, token-storage, and local-authorization dependencies from entering shared UI.

using System.Text.RegularExpressions;

namespace Event.Architecture.Tests;

public sealed class EventControlPlaneClientArchitectureTests
{
    private static readonly string RepoRoot = ResolveRepoRoot();
    private static readonly string ControlPlaneClientRoot = Path.Combine(RepoRoot, "Event.ControlPlane.Client");

    [Test]
    public async Task EventControlPlaneClient_Project_MustExistAsHostNeutralRazorClassLibrary()
    {
        var projectPath = Path.Combine(ControlPlaneClientRoot, "Event.ControlPlane.Client.csproj");
        await Assert.That(File.Exists(projectPath)).IsTrue()
            .Because("Event.ControlPlane.Client must exist as the shared control-plane Razor class library.");

        var projectXml = await File.ReadAllTextAsync(projectPath);
        await Assert.That(projectXml.Contains("Microsoft.NET.Sdk.Razor", StringComparison.Ordinal)).IsTrue()
            .Because("Event.ControlPlane.Client must be a Razor class library for shared Blazor components.");

        await Assert.That(projectXml.Contains("<ProjectReference", StringComparison.OrdinalIgnoreCase)).IsFalse()
            .Because("Event.ControlPlane.Client must stay host-neutral and cannot reference app, API, Application, Domain, Persistence, or Infrastructure projects.");
    }

    [Test]
    public async Task EventControlPlaneClient_Source_MustNotDependOnForbiddenBoundaries()
    {
        var forbiddenTokens = new[]
        {
            "Explore.Blazor.Client",
            "Explore.Blazor",
            "Explore.API",
            "Explore.Application",
            "Explore.Domain",
            "Explore.Infrastructure",
            "Explore.Persistence",
            "Event.Web.BffHosting",
            "Event.ControlPlane.Blazor",
            "EventApiClient",
            "IEventApiClient",
            "ApiException",
            "HttpClient",
            "IHttpClientFactory",
            "System.Net.Http",
            "JsonContent",
            "ReadFromJsonAsync",
            "GetFromJsonAsync",
            "EnsureSuccessStatusCode",
            "AccessToken",
            "RefreshToken",
            "localStorage",
            "sessionStorage",
            "ProtectedLocalStorage",
            "AuthenticationStateProvider",
            "ClaimsPrincipal",
            "AuthorizeView",
            "IAuthorizationService",
            "IsInRole"
        };

        var violations = new List<string>();
        foreach (var file in EnumerateSourceFiles(ControlPlaneClientRoot))
        {
            var content = await File.ReadAllTextAsync(file);
            var relative = Path.GetRelativePath(RepoRoot, file).Replace('\\', '/');
            foreach (var token in forbiddenTokens)
            {
                if (content.Contains(token, StringComparison.Ordinal))
                {
                    violations.Add($"{relative} contains forbidden dependency token '{token}'");
                }
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because(string.Join('\n', violations));
    }

    [Test]
    public async Task ControlPlaneRoutes_MustStayUnderAdminPortalRoots()
    {
        var routesPath = Path.Combine(ControlPlaneClientRoot, "Routing", "ControlPlaneRoutes.cs");
        await Assert.That(File.Exists(routesPath)).IsTrue()
            .Because("Control-plane route constants must be centralized.");

        var source = await File.ReadAllTextAsync(routesPath);
        await Assert.That(source.Contains("public const string Root = \"/admin/instance\";", StringComparison.Ordinal)).IsTrue()
            .Because("Control-plane routes must live under the instance administration root.");
        await Assert.That(source.Contains("public const string TenantRoot = \"/tenant/{TenantSlug}\";", StringComparison.Ordinal)).IsTrue()
            .Because("Tenant-console routes must live under the dedicated tenant administration root.");

        var directRouteLiterals = Regex.Matches(source, "public const string (?<name>\\w+) = \"(?<path>/[^\"]+)\";")
            .Select(match => match.Groups["path"].Value)
            .ToArray();

        var directRouteViolations = directRouteLiterals
            .Where(route => !route.StartsWith("/admin/instance", StringComparison.Ordinal)
                && !route.StartsWith("/tenant/{TenantSlug}", StringComparison.Ordinal))
            .ToArray();

        await Assert.That(directRouteViolations).IsEmpty()
            .Because($"AdminPortal routes must not introduce public paths outside the instance or tenant administration roots. Violations: {string.Join(", ", directRouteViolations)}");

        var nonRootConstants = Regex.Matches(source, "public const string (?<name>\\w+) = (?<expression>[^;]+);")
            .Select(match => new
            {
                Name = match.Groups["name"].Value,
                Expression = match.Groups["expression"].Value.Trim()
            })
            .Where(route => route.Name is not "Root" and not "TenantRoot")
            .ToArray();

        var compositionViolations = nonRootConstants
            .Where(route => route.Expression != "Root"
                && route.Expression != "TenantRoot"
                && !route.Expression.StartsWith("Root +", StringComparison.Ordinal)
                && !route.Expression.StartsWith("TenantRoot +", StringComparison.Ordinal))
            .Select(route => $"{route.Name} = {route.Expression}")
            .ToArray();

        await Assert.That(compositionViolations).IsEmpty()
            .Because($"AdminPortal child routes must compose from Root or TenantRoot. Violations: {string.Join(", ", compositionViolations)}");
    }

    [Test]
    public async Task EventControlPlaneClient_MustExposeSharedServiceRegistration()
    {
        var extensionPath = Path.Combine(ControlPlaneClientRoot, "Extensions", "ServiceCollectionExtensions.cs");
        await Assert.That(File.Exists(extensionPath)).IsTrue()
            .Because("Blazor hosts need one shared registration entry point for control-plane client services.");

        var source = await File.ReadAllTextAsync(extensionPath);
        await Assert.That(source.Contains("AddEventControlPlaneClient", StringComparison.Ordinal)).IsTrue();
        await Assert.That(source.Contains("IControlPlaneRouteCatalog", StringComparison.Ordinal)).IsTrue()
            .Because("The first shared service is the route catalog used by embedded and separate hosts.");

        await Assert.That(source.Contains("IControlPlaneOverviewService", StringComparison.Ordinal)).IsTrue()
            .Because("Overview pages must depend on a host-provided service contract, not a generated client.");
        await Assert.That(source.Contains("IControlPlaneTenantService", StringComparison.Ordinal)).IsTrue()
            .Because("Tenant pages must depend on a host-provided service contract, not a generated client.");
        await Assert.That(source.Contains("IControlPlaneDomainService", StringComparison.Ordinal)).IsTrue()
            .Because("Domain pages must depend on a host-provided service contract, not a generated client.");
    }

    [Test]
    public async Task EventControlPlaneClient_MustExposeHalAndFailureContracts()
    {
        var requiredFiles = new[]
        {
            Path.Combine(ControlPlaneClientRoot, "Contracts", "ControlPlaneHalLink.cs"),
            Path.Combine(ControlPlaneClientRoot, "Contracts", "IControlPlaneHalResource.cs"),
            Path.Combine(ControlPlaneClientRoot, "Contracts", "ControlPlaneLinkRelations.cs"),
            Path.Combine(ControlPlaneClientRoot, "Contracts", "ControlPlaneResult.cs"),
            Path.Combine(ControlPlaneClientRoot, "Contracts", "ControlPlaneCommandResult.cs"),
            Path.Combine(ControlPlaneClientRoot, "Services", "IControlPlaneOverviewService.cs"),
            Path.Combine(ControlPlaneClientRoot, "Services", "IControlPlaneTenantService.cs"),
            Path.Combine(ControlPlaneClientRoot, "Services", "IControlPlaneDomainService.cs")
        };

        var missing = requiredFiles
            .Where(file => !File.Exists(file))
            .Select(file => Path.GetRelativePath(RepoRoot, file).Replace('\\', '/'))
            .ToArray();

        await Assert.That(missing).IsEmpty()
            .Because($"Shared control-plane components need HAL, failure-state, and adapter-driven service contracts. Missing: {string.Join(", ", missing)}");

        var resultSource = await File.ReadAllTextAsync(Path.Combine(ControlPlaneClientRoot, "Contracts", "ControlPlaneResult.cs"));
        await Assert.That(resultSource.Contains("ControlPlaneResultKind", StringComparison.Ordinal)).IsTrue()
            .Because("Control-plane services must return explicit failure states.");
        await Assert.That(resultSource.Contains("ControlPlaneProblem", StringComparison.Ordinal)).IsTrue()
            .Because("Control-plane services must expose safe problem details without raw transport exceptions.");

        var halSource = await File.ReadAllTextAsync(Path.Combine(ControlPlaneClientRoot, "Contracts", "ControlPlaneHal.cs"));
        await Assert.That(halSource.Contains("HasLink", StringComparison.Ordinal)).IsTrue()
            .Because("Control-plane components must gate action affordances from HAL link presence.");
    }

    [Test]
    public async Task EventControlPlaneClient_DesignSystem_MustUseLocalPrimitivesWithoutPublicWrapperCoupling()
    {
        var projectPath = Path.Combine(ControlPlaneClientRoot, "Event.ControlPlane.Client.csproj");
        var importsPath = Path.Combine(ControlPlaneClientRoot, "_Imports.razor");
        var requiredFiles = new[]
        {
            Path.Combine(ControlPlaneClientRoot, "Components", "Common", "ControlPlaneActionButton.razor"),
            Path.Combine(ControlPlaneClientRoot, "Components", "Common", "ControlPlaneActionButton.razor.css"),
            Path.Combine(ControlPlaneClientRoot, "Components", "Common", "ControlPlanePageHeader.razor"),
            Path.Combine(ControlPlaneClientRoot, "Components", "Common", "ControlPlanePageHeader.razor.css"),
            Path.Combine(ControlPlaneClientRoot, "Components", "Common", "ControlPlanePanel.razor"),
            Path.Combine(ControlPlaneClientRoot, "Components", "Common", "ControlPlanePanel.razor.css")
        };

        var missing = requiredFiles
            .Where(file => !File.Exists(file))
            .Select(file => Path.GetRelativePath(RepoRoot, file).Replace('\\', '/'))
            .ToArray();

        await Assert.That(missing).IsEmpty()
            .Because($"The control-plane RCL needs local design-system primitives before pages are added. Missing: {string.Join(", ", missing)}");

        var projectXml = await File.ReadAllTextAsync(projectPath);
        await Assert.That(projectXml.Contains("<PackageReference Include=\"MudBlazor\" />", StringComparison.Ordinal)).IsTrue()
            .Because("Control-plane primitives may use MudBlazor directly, but the RCL must not depend on Explore.Blazor.Client wrappers.");

        var imports = await File.ReadAllTextAsync(importsPath);
        await Assert.That(imports.Contains("@using MudBlazor", StringComparison.Ordinal)).IsTrue()
            .Because("Shared control-plane primitives should have a single MudBlazor import point.");
        await Assert.That(imports.Contains("@using Event.ControlPlane.Client.Components.Common", StringComparison.Ordinal)).IsTrue()
            .Because("Shared pages should consume local ControlPlane* primitives without per-page using churn.");

        var componentNameViolations = new List<string>();
        foreach (var file in EnumerateSourceFiles(Path.Combine(ControlPlaneClientRoot, "Components")))
        {
            var relative = Path.GetRelativePath(RepoRoot, file).Replace('\\', '/');
            var fileName = Path.GetFileNameWithoutExtension(file);
            var content = await File.ReadAllTextAsync(file);

            if (fileName.StartsWith("App", StringComparison.Ordinal))
            {
                componentNameViolations.Add($"{relative} uses public-app wrapper naming");
            }

            if (content.Contains("Explore.Blazor.Client.Components.Common", StringComparison.Ordinal)
                || content.Contains("<AppButton", StringComparison.Ordinal)
                || content.Contains("<AppCard", StringComparison.Ordinal)
                || content.Contains("<AppIconButton", StringComparison.Ordinal)
                || content.Contains("<AppTextField", StringComparison.Ordinal)
                || content.Contains("<AppDialogShell", StringComparison.Ordinal))
            {
                componentNameViolations.Add($"{relative} couples to public-app wrapper components");
            }

            var cssPair = Path.ChangeExtension(file, ".razor.css");
            if (!File.Exists(cssPair))
            {
                componentNameViolations.Add($"{relative} is missing a colocated CSS isolation file");
            }
        }

        await Assert.That(componentNameViolations).IsEmpty()
            .Because(string.Join('\n', componentNameViolations));

        var cssViolations = new List<string>();
        foreach (var file in EnumerateCssFiles(ControlPlaneClientRoot))
        {
            var content = await File.ReadAllTextAsync(file);
            var relative = Path.GetRelativePath(RepoRoot, file).Replace('\\', '/');

            if (!content.Contains("ABOUTME:", StringComparison.Ordinal))
            {
                cssViolations.Add($"{relative} is missing ABOUTME header comments");
            }

            if (!content.Contains(".control-plane-", StringComparison.Ordinal))
            {
                cssViolations.Add($"{relative} must use the control-plane BEM namespace");
            }

            if (Regex.IsMatch(content, @"(?m)^\s*\.mud-", RegexOptions.CultureInvariant))
            {
                cssViolations.Add($"{relative} contains a bare MudBlazor selector instead of scoped ::deep usage");
            }

            var physicalDirectionTokens = new[]
            {
                "margin-left",
                "margin-right",
                "padding-left",
                "padding-right",
                "border-left",
                "border-right",
                "left:",
                "right:",
                "text-align: left",
                "text-align: right"
            };

            foreach (var token in physicalDirectionTokens)
            {
                if (content.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    cssViolations.Add($"{relative} contains physical direction token '{token}'");
                }
            }
        }

        await Assert.That(cssViolations).IsEmpty()
            .Because(string.Join('\n', cssViolations));
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root) =>
        Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            .Where(file => !IsGeneratedOrBuildOutput(file));

    private static IEnumerable<string> EnumerateCssFiles(string root) =>
        Directory.EnumerateFiles(root, "*.css", SearchOption.AllDirectories)
            .Where(file => !IsGeneratedOrBuildOutput(file));

    private static bool IsGeneratedOrBuildOutput(string file)
    {
        var normalized = file.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.Ordinal)
            || normalized.Contains("/obj/", StringComparison.Ordinal)
            || normalized.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Explore.sln"))
                && Directory.Exists(Path.Combine(current.FullName, "Event.Web.BffHosting")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing Explore.sln and Event.Web.BffHosting.");
    }
}
