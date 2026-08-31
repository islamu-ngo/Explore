// ABOUTME: Defines Setup Assistant project, dependency, capability, ratchet, and CI governance boundaries.
// ABOUTME: Enforces package-free scaffolds, read-only routing, and source-versus-output tracking semantics.

namespace Event.Architecture.Tests;

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using Explore.Application.Features.ConfigurationManifest.Catalog;
using Explore.Application.Features.ConfigurationManifest.Importing;

public sealed class SetupAssistantArchitectureTests
{
    private const string BrowserCapabilityPath =
        "eng/setup-assistant/generated/browser-release-capabilities.json";
    private const string FrozenContractBaselinePath =
        "eng/setup-assistant/generated/frozen-contract-baseline.json";
    private const string ManifestSchemaId =
        "https://schemas.islamu.org/event/configuration-manifest/v1alpha2/schema.json";
    private const string TenantPackageSchemaId =
        "https://schemas.islamu.org/event/tenant-configuration-package/v1alpha2/schema.json";
    private const string ApiVersion = "configuration.islamu.org/v1alpha2";

    private static readonly string[] RegistryKeys =
    [
        "instance.settings", "instance.documents", "instance.legal_documents",
        "tenant.settings", "tenant.documents", "tenant.legal_documents",
        "tenant.footer", "tenant.navigation", "tenant.templates", "tenant.lookups",
        "tenant.custom_property_definitions", "tenant.localization",
        "tenant.registration_policy", "tenant.modules", "extensions",
        "excluded.secrets", "excluded.pii", "excluded.application_data",
        "excluded.operational_state", "excluded.provider_bindings",
        "excluded.deployment_topology"
    ];

    private static readonly string[] ExcludedRegistryKeys =
    [
        "excluded.secrets", "excluded.pii", "excluded.application_data",
        "excluded.operational_state", "excluded.provider_bindings",
        "excluded.deployment_topology"
    ];

    private static readonly IReadOnlyDictionary<string, string[]> AllowedProjectReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Event.Setup.Core"] = ["Event.Wire.Contracts"],
            ["Event.SetupAssistant"] = ["Event.Setup.Core"],
            ["Event.SetupAssistant.Browser"] = ["Event.SetupAssistant"],
            ["Event.SetupAssistant.Desktop"] = ["Event.SetupAssistant"],
            ["Event.SetupAssistant.Cli"] = ["Event.Setup.Core"],
            ["SetupCliCommandSchemaGenerator"] = ["Event.SetupAssistant.Cli"],
            ["Event.Setup.Core.Tests"] = ["Event.Setup.Core"],
            ["Event.SetupAssistant.Tests"] = ["Event.SetupAssistant"],
            ["Event.SetupAssistant.Browser.Tests"] = ["Event.SetupAssistant.Browser"],
            ["Event.SetupAssistant.Desktop.Tests"] = ["Event.SetupAssistant.Desktop"],
            ["Event.SetupAssistant.Cli.Tests"] = ["Event.SetupAssistant.Cli"]
        };

    private static readonly IReadOnlyDictionary<string, string> RequiredSolutionProjects =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Event.Setup.Core"] = "src/Event.Setup.Core/Event.Setup.Core.csproj",
            ["Event.SetupAssistant"] = "src/Event.SetupAssistant/Event.SetupAssistant.csproj",
            ["Event.SetupAssistant.Browser"] = "src/Event.SetupAssistant.Browser/Event.SetupAssistant.Browser.csproj",
            ["Event.SetupAssistant.Desktop"] = "src/Event.SetupAssistant.Desktop/Event.SetupAssistant.Desktop.csproj",
            ["Event.SetupAssistant.Cli"] = "src/Event.SetupAssistant.Cli/Event.SetupAssistant.Cli.csproj",
            ["SetupCliCommandSchemaGenerator"] = "eng/setup-assistant/SetupCliCommandSchemaGenerator/SetupCliCommandSchemaGenerator.csproj",
            ["Event.Setup.Core.Tests"] = "tests/Event.Setup.Core.Tests/Event.Setup.Core.Tests.csproj",
            ["Event.SetupAssistant.Tests"] = "tests/Event.SetupAssistant.Tests/Event.SetupAssistant.Tests.csproj",
            ["Event.SetupAssistant.Browser.Tests"] = "tests/Event.SetupAssistant.Browser.Tests/Event.SetupAssistant.Browser.Tests.csproj",
            ["Event.SetupAssistant.Desktop.Tests"] = "tests/Event.SetupAssistant.Desktop.Tests/Event.SetupAssistant.Desktop.Tests.csproj",
            ["Event.SetupAssistant.Cli.Tests"] = "tests/Event.SetupAssistant.Cli.Tests/Event.SetupAssistant.Cli.Tests.csproj"
        };

    private static readonly string[] ApprovedTestPackages =
        ["TUnit", "bunit", "NSubstitute", "Verify.TUnit"];

    private static readonly string[] BlockedPackageTerms =
        ["Terminal.Gui", "Avalonia", "Sharprompt"];

    private static readonly string[] ForbiddenSourcePackageTerms =
    [
        "EntityFrameworkCore", "MediatR", "Infisical", "KeyVault",
        "SecretsManager", "HashiCorp.Vault", "AWSSDK", "Amazon.", "Azure.",
        "Google.Cloud", "Stripe", "PayPal", "Braintree", "Adyen", "HttpClient",
        "Refit", "RestSharp", "Flurl", "Grpc.Net.Client", "GraphQL.Client",
        "WebSocket", "OpenTelemetry", "ApplicationInsights", "Sentry", "NewRelic",
        "Datadog", "Analytics", "Telemetry", "Microsoft.Extensions.AI", "OpenAI",
        "Anthropic", "SemanticKernel", "ML.NET", "Onnx", "ModelContextProtocol",
        "Avalonia.Diagnostics", "Avalonia.Designer", "Avalonia.Professional",
        "Avalonia.Commercial"
    ];

    [Test]
    public async Task PlannedProjects_MustExistInSolutionWithExactInwardDependencies()
    {
        XDocument solution = XDocument.Load(ContextSystemHelpers.RepoPath("Explore.slnx"));
        string[] solutionPaths = solution.Descendants()
            .Where(element => element.Name.LocalName == "Project")
            .Select(element => NormalizePath(element.Attribute("Path")?.Value ?? string.Empty))
            .ToArray();
        string[] missing = RequiredSolutionProjects.Values
            .Where(required => solutionPaths.Count(actual =>
                string.Equals(actual, required, StringComparison.Ordinal)) != 1)
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "Missing planned Setup source/test projects in Explore.slnx: "
                + string.Join(", ", missing));
        }

        var projects = RequiredSolutionProjects.ToDictionary(
            pair => pair.Key,
            pair => XDocument.Load(ContextSystemHelpers.RepoPath(
                pair.Value.Split('/', StringSplitOptions.RemoveEmptyEntries))),
            StringComparer.Ordinal);
        string[] violations = ValidateProjectMetadata(projects);

        await Assert.That(violations).IsEmpty()
            .Because("Setup projects must preserve the exact offline inward graph and approved package boundary: "
                + string.Join("; ", violations));
    }

    [Test]
    public async Task SetupLocks_MustExistAndExcludeBlockedUiPackages()
    {
        var violations = new List<string>();
        foreach (string projectPath in RequiredSolutionProjects.Values)
        {
            string relativeLockPath =
                $"{NormalizePath(Path.GetDirectoryName(projectPath)!)}/packages.lock.json";
            string lockPath = ContextSystemHelpers.RepoPath(
                relativeLockPath.Split('/', StringSplitOptions.RemoveEmptyEntries));
            if (!File.Exists(lockPath))
            {
                violations.Add($"missing lock: {relativeLockPath}");
                continue;
            }

            string lockContent = await File.ReadAllTextAsync(lockPath);
            violations.AddRange(BlockedPackageTerms
                .Where(term => lockContent.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(term => $"blocked package term {term} in {relativeLockPath}"));
        }

        await Assert.That(violations).IsEmpty()
            .Because("all ten Setup locks must remain discoverable and package-free of blocked UI graphs: "
                + string.Join("; ", violations));
    }

    [Test]
    public async Task BrowserReleaseCapability_MustExistAndDisableSecretsByDefault()
    {
        string path = ContextSystemHelpers.RepoPath(
            BrowserCapabilityPath.Split('/', StringSplitOptions.RemoveEmptyEntries));
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"Missing planned Setup browser release-capability artifact: {BrowserCapabilityPath}");
        }

        using JsonDocument document = JsonDocument.Parse(await File.ReadAllBytesAsync(path));
        string[] violations = ValidateBrowserCapability(document.RootElement);
        await Assert.That(violations).IsEmpty()
            .Because("The generated browser release capability must fail closed: "
                + string.Join("; ", violations));
    }

    [Test]
    public async Task FrozenContractBaseline_MustExistAndIdentifyTheExtractionBoundary()
    {
        string path = ContextSystemHelpers.RepoPath(
            FrozenContractBaselinePath.Split('/', StringSplitOptions.RemoveEmptyEntries));
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"Missing planned Setup frozen-contract baseline artifact: {FrozenContractBaselinePath}");
        }

        using JsonDocument document = JsonDocument.Parse(await File.ReadAllBytesAsync(path));
        string[] violations = ValidateFrozenContractBaseline(document.RootElement);
        await Assert.That(violations).IsEmpty()
            .Because("The generated Setup extraction baseline must identify the frozen public contract: "
                + string.Join("; ", violations));
    }

    [Test]
    public async Task CurrentFrozenBaseline_MustRemainHealthyBeforeExtraction()
    {
        ValidateSchema(
            "schemas/configuration-manifest-v1alpha2.schema.json",
            ManifestSchemaId,
            "ConfigurationManifest");
        ValidateSchema(
            "schemas/tenant-configuration-package-v1alpha2.schema.json",
            TenantPackageSchemaId,
            "TenantConfigurationPackage");

        await Assert.That(ConfigurationPortabilityRegistry.Sections.Count).IsEqualTo(21);
        await Assert.That(ConfigurationPortabilityRegistry.Sections.Keys)
            .IsEquivalentTo(RegistryKeys);

        foreach (string key in ExcludedRegistryKeys)
        {
            ConfigurationPortabilitySectionDescriptor descriptor =
                ConfigurationPortabilityRegistry.Sections[key];
            await Assert.That(descriptor.Scope).IsEqualTo(ConfigurationPortabilityScope.Excluded);
            await Assert.That(descriptor.Authority).IsEqualTo(ConfigurationPortabilityAuthority.None);
            await Assert.That(descriptor.ArtifactKinds).IsEmpty();
            await Assert.That(new[]
            {
                descriptor.SupportsExport, descriptor.SupportsPreview,
                descriptor.SupportsDiff, descriptor.SupportsApply,
                descriptor.SupportsVerify, descriptor.SupportsRollback,
                descriptor.SupportsDeletion
            }.Any(value => value)).IsFalse();
            await Assert.That(descriptor.OmissionReasonCode).IsNotEmpty();
        }

        string[] targetProperties = PublicPropertyNames(typeof(ConfigurationImportTarget));
        string[] artifactProperties = PublicPropertyNames(
            typeof(ConfigurationImportArtifactReference));
        string[] bindingProperties = PublicPropertyNames(
            typeof(ConfigurationImportPreviewBinding));
        string[] sessionProperties = PublicPropertyNames(typeof(ConfigurationImportSession));

        await Assert.That(targetProperties)
            .IsEquivalentTo(["AuthorityKey", "Scope", "TenantId"]);
        await Assert.That(artifactProperties)
            .IsEquivalentTo(["ByteLength", "ExpiresAt", "Handle", "Sha256Digest"]);
        await Assert.That(bindingProperties).IsEquivalentTo(BindingProperties);
        await Assert.That(typeof(ConfigurationImportPreviewBinding).GetMethod(
            "Matches", BindingFlags.Public | BindingFlags.Instance)).IsNotNull();
        await Assert.That(typeof(ConfigurationImportSession).GetMethod(
            "MatchesTarget", BindingFlags.Public | BindingFlags.Instance)).IsNotNull();
        await Assert.That(sessionProperties.Concat(artifactProperties)
            .Concat(bindingProperties).Any(IsValueBearingPropertyName)).IsFalse();
    }

    [Test]
    public async Task ProjectBoundaryVerifier_MustAcceptCompliantAndRejectForbiddenMetadata()
    {
        IReadOnlyDictionary<string, XDocument> compliant = CreateCompliantFixture();
        var bad = compliant.ToDictionary(
            pair => pair.Key,
            pair => XDocument.Parse(pair.Value.ToString()),
            StringComparer.Ordinal);
        bad["Event.Setup.Core"] = XDocument.Parse(
            """
            <Project><ItemGroup>
              <ProjectReference Include="../Explore.API/Explore.API.csproj" />
              <PackageReference Include="OpenTelemetry" />
            </ItemGroup></Project>
            """);
        bad["Event.Setup.Core.Tests"] = XDocument.Parse(
            """
            <Project><ItemGroup>
              <ProjectReference Include="../../src/Event.SetupAssistant/Event.SetupAssistant.csproj" />
              <PackageReference Include="TUnit" />
            </ItemGroup></Project>
            """);

        string[] compliantViolations = ValidateProjectMetadata(compliant);
        string[] badViolations = ValidateProjectMetadata(bad);

        await Assert.That(compliantViolations).IsEmpty();
        await Assert.That(badViolations.Any(value =>
            value.Contains("Event.Setup.Core -> Explore.API", StringComparison.Ordinal))).IsTrue();
        await Assert.That(badViolations.Any(value =>
            value.Contains("PackageReference Event.Setup.Core: OpenTelemetry", StringComparison.Ordinal))).IsTrue();
        await Assert.That(badViolations.Any(value =>
            value.Contains("Event.Setup.Core.Tests -> Event.SetupAssistant", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task StructuredRatchetVerifiers_MustAcceptClosedAndRejectUnsafeFixtures()
    {
        using JsonDocument safeCapability = JsonDocument.Parse(
            """{"schemaVersion":1,"target":"browser","capabilities":{"secretEntry":false}}""");
        using JsonDocument unsafeCapability = JsonDocument.Parse(
            """{"schemaVersion":1,"target":"browser","capabilities":{"secretEntry":true}}""");
        using JsonDocument safeBaseline = JsonDocument.Parse(CreateBaselineFixture(21, true));
        using JsonDocument unsafeBaseline = JsonDocument.Parse(CreateBaselineFixture(22, false));

        await Assert.That(ValidateBrowserCapability(safeCapability.RootElement)).IsEmpty();
        await Assert.That(ValidateBrowserCapability(unsafeCapability.RootElement))
            .Contains("capabilities.secretEntry must be false");
        await Assert.That(ValidateFrozenContractBaseline(safeBaseline.RootElement)).IsEmpty();
        await Assert.That(ValidateFrozenContractBaseline(unsafeBaseline.RootElement)).IsNotEmpty();
    }

    [Test]
    public async Task SetupCiGovernance_MustRouteReadOnlyChecksAndRejectUnsafeFixtures()
    {
        string caller = await File.ReadAllTextAsync(ContextSystemHelpers.RepoPath(
            ".github", "workflows", "test.yml"));
        string reusable = await File.ReadAllTextAsync(ContextSystemHelpers.RepoPath(
            ".github", "workflows", "_build-test.yml"));
        string[] actual = ValidateCiGovernance(caller, reusable);
        string[] unsafeFixture = ValidateCiGovernance(
            "permissions:\n  contents: write\nsecrets: inherit\n",
            "permissions:\n  id-token: write\npersist-credentials: true\n--write\n");

        await Assert.That(actual).IsEmpty()
            .Because("Setup CI must be read-only, always routed, and non-mutating: "
                + string.Join("; ", actual));
        await Assert.That(unsafeFixture).IsNotEmpty();
    }

    [Test]
    public async Task SetupRepositoryPaths_MustTrackInputsAndIgnoreOnlyGeneratedOutputs()
    {
        string[] trackablePaths =
        [
            "src/Event.Setup.Core/Event.Setup.Core.csproj",
            "src/Event.SetupAssistant/Event.SetupAssistant.csproj",
            "src/Event.SetupAssistant.Browser/Event.SetupAssistant.Browser.csproj",
            "src/Event.SetupAssistant.Desktop/Event.SetupAssistant.Desktop.csproj",
            "src/Event.SetupAssistant.Cli/Program.cs",
            "tests/Event.Setup.Core.Tests/Program.cs",
            .. RequiredSolutionProjects.Values.Select(path =>
                $"{NormalizePath(Path.GetDirectoryName(path)!)}/packages.lock.json"),
            "eng/setup-assistant/GenerateSetupAssistantRatchets.cs",
            "eng/setup-assistant/SetupCliCommandSchemaGenerator/Program.cs",
            BrowserCapabilityPath,
            FrozenContractBaselinePath,
            "src/Event.SetupAssistant.Browser/wwwroot/index.html"
        ];
        string[] ignoredPaths =
        [
            "src/Event.SetupAssistant/bin/Release/net10.0/output.dll",
            "src/Event.SetupAssistant/obj/project.assets.json",
            "src/Event.SetupAssistant.Browser/bin/Release/net10.0/wwwroot/index.html",
            "src/Event.SetupAssistant.Browser/publish/site.zip",
            "src/Event.SetupAssistant/packages/staging/package.nupkg",
            "releases/setup-assistant.zip",
            "artifacts/setup-assistant/report.json"
        ];

        string[] unexpectedlyIgnored = trackablePaths.Where(IsIgnoredByGit).ToArray();
        string[] unexpectedlyTrackable = ignoredPaths.Where(path => !IsIgnoredByGit(path)).ToArray();

        await Assert.That(unexpectedlyIgnored).IsEmpty()
            .Because("Setup source, locks, generator inputs, ratchets, and future browser source must remain trackable: "
                + string.Join(", ", unexpectedlyIgnored));
        await Assert.That(unexpectedlyTrackable).IsEmpty()
            .Because("Setup build, publish, staging, release, and artifact outputs must stay ignored: "
                + string.Join(", ", unexpectedlyTrackable));
    }

    [Test]
    public async Task DependencyLicenseCommand_MustDiscoverNestedSetupLocks()
    {
        string temporaryRoot = Path.Combine(
            Path.GetTempPath(), $"event-sa130-license-{Guid.NewGuid():N}");
        string nested = Path.Combine(temporaryRoot, "src", "Event.Setup.Future");
        Directory.CreateDirectory(nested);
        await File.WriteAllTextAsync(
            Path.Combine(nested, "packages.lock.json"),
            """
            {"version":1,"dependencies":{"net10.0":{"SA130.LockDiscovery.Probe":{"type":"Direct","requested":"[1.0.0, )","resolved":"1.0.0","contentHash":""}}}}
            """);

        try
        {
            ProcessResult result = RunProcess(
                "dotnet",
                ["run", ContextSystemHelpers.RepoPath(".ci", "scripts",
                    "validate-dependency-license-policy.cs"), "--", temporaryRoot],
                ContextSystemHelpers.RepoPath());

            await Assert.That(result.ExitCode).IsNotEqualTo(0);
            await Assert.That(result.Output).Contains("SA130.LockDiscovery.Probe 1.0.0");
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Test]
    public async Task SetupTerminalBoundary_MustKeepAmbientAndSecretAuthorityInExactOwners()
    {
        string root = ContextSystemHelpers.RepoPath("src", "Event.SetupAssistant.Cli");
        string[] files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();
        var violations = new List<string>();
        foreach (string file in files)
        {
            string relative = NormalizePath(Path.GetRelativePath(root, file));
            string source = await File.ReadAllTextAsync(file);
            bool consoleOwner = relative is "Program.cs" or "Tui/ConsoleSetupTerminalDriver.cs";
            if (!consoleOwner && (source.Contains("Console.", StringComparison.Ordinal)
                || source.Contains("PosixSignal", StringComparison.Ordinal)))
                violations.Add($"ambient terminal API outside owner: {relative}");
            bool fileOwner = relative is "Program.cs" or "Tui/UnixSetupTerminalProtectedWriter.cs";
            if (!fileOwner && (source.Contains("File.", StringComparison.Ordinal)
                || source.Contains("FileStream", StringComparison.Ordinal)
                || source.Contains("Directory.", StringComparison.Ordinal)))
                violations.Add($"filesystem API outside owner: {relative}");
            if (source.Contains("new Thread", StringComparison.Ordinal)
                && relative != "Tui/SetupTerminalReadCoordinator.cs")
                violations.Add($"terminal reader thread outside coordinator: {relative}");
            if (source.Contains("LocalSecretGenerator", StringComparison.Ordinal)
                && relative != "Tui/SetupTerminalSession.cs")
                violations.Add($"local generator outside TUI workflow: {relative}");
            foreach (string forbidden in new[] { "ReadLine(", "KeyAvailable", "Clipboard", "Autosave", "System.Diagnostics.Process", "DllImport", "LibraryImport" })
                if (source.Contains(forbidden, StringComparison.Ordinal)) violations.Add($"forbidden terminal API {forbidden}: {relative}");
        }

        await Assert.That(violations).IsEmpty().Because(string.Join("; ", violations));
    }

    private static readonly string[] BindingProperties =
    [
        "ApplyMode", "ArtifactDigest", "ExpiresAt", "MappingDigest",
        "RequiredApprovalDigest", "SelectedSectionsDigest", "Target",
        "TargetRevisionDigest"
    ];

    private static string[] ValidateProjectMetadata(
        IReadOnlyDictionary<string, XDocument> projects)
    {
        var violations = new List<string>();
        foreach ((string projectName, string[] expectedReferences) in AllowedProjectReferences)
        {
            if (!projects.TryGetValue(projectName, out XDocument? project))
            {
                violations.Add($"missing project metadata: {projectName}");
                continue;
            }

            string[] actualReferences = project.Descendants()
                .Where(element => element.Name.LocalName == "ProjectReference")
                .Select(element => Path.GetFileNameWithoutExtension(
                    NormalizePath(element.Attribute("Include")?.Value ?? string.Empty)))
                .ToArray();
            violations.AddRange(actualReferences.Except(expectedReferences, StringComparer.Ordinal)
                .Select(reference => $"forbidden ProjectReference {projectName} -> {reference}"));
            violations.AddRange(expectedReferences.Except(actualReferences, StringComparer.Ordinal)
                .Select(reference => $"missing ProjectReference {projectName} -> {reference}"));

            string[] packages = project.Descendants()
                .Where(element => element.Name.LocalName == "PackageReference")
                .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
                .ToArray();
            violations.AddRange(packages
                .Where(package => BlockedPackageTerms.Any(term =>
                    package.Contains(term, StringComparison.OrdinalIgnoreCase)))
                .Select(package => $"blocked PackageReference {projectName}: {package}"));
            if (projectName.EndsWith(".Tests", StringComparison.Ordinal))
            {
                violations.AddRange(packages
                    .Where(package => !ApprovedTestPackages.Contains(package, StringComparer.Ordinal))
                    .Select(package =>
                        $"unapproved test PackageReference {projectName}: {package}"));
            }
            else
            {
                violations.AddRange(packages
                    .Where(package => ForbiddenSourcePackageTerms.Any(term =>
                        package.Contains(term, StringComparison.OrdinalIgnoreCase)))
                    .Select(package =>
                        $"forbidden source PackageReference {projectName}: {package}"));
            }
        }
        return [.. violations];
    }

    private static string[] ValidateBrowserCapability(JsonElement root)
    {
        var violations = new List<string>();
        if (!TryGetInt32(root, "schemaVersion", out int version) || version != 1)
            violations.Add("schemaVersion must be 1");
        if (!TryGetString(root, "target", out string? target)
            || !string.Equals(target, "browser", StringComparison.Ordinal))
        {
            violations.Add("target must be browser");
        }
        if (!root.TryGetProperty("capabilities", out JsonElement capabilities)
            || capabilities.ValueKind != JsonValueKind.Object
            || !capabilities.TryGetProperty("secretEntry", out JsonElement secretEntry)
            || secretEntry.ValueKind != JsonValueKind.False)
        {
            violations.Add("capabilities.secretEntry must be false");
        }
        return [.. violations];
    }

    private static string[] ValidateFrozenContractBaseline(JsonElement root)
    {
        var violations = new List<string>();
        if (!TryGetInt32(root, "schemaVersion", out int version) || version != 1)
            violations.Add("schemaVersion must be 1");
        ValidateBaselineSchema(root, "configurationManifest", ManifestSchemaId,
            "ConfigurationManifest", violations);
        ValidateBaselineSchema(root, "tenantConfigurationPackage", TenantPackageSchemaId,
            "TenantConfigurationPackage", violations);

        if (!TryGetNested(root, out JsonElement registry, "portabilityRegistry"))
        {
            violations.Add("portabilityRegistry is missing");
        }
        else
        {
            if (!TryGetInt32(registry, "cardinality", out int cardinality) || cardinality != 21)
                violations.Add("portabilityRegistry.cardinality must be 21");
            ValidateExactArray(registry, "keys", RegistryKeys, violations);
            ValidateExactArray(registry, "excludedAuthorityKeys", ExcludedRegistryKeys, violations);
        }

        ValidateContractFacts(root, "importSession", "targetProperties",
            ["AuthorityKey", "Scope", "TenantId"], violations);
        ValidateContractFacts(root, "importPreview", "bindingProperties",
            BindingProperties, violations);
        return [.. violations];
    }

    private static void ValidateBaselineSchema(
        JsonElement root,
        string name,
        string schemaId,
        string kind,
        ICollection<string> violations)
    {
        if (!TryGetNested(root, out JsonElement schema, "schemas", name))
        {
            violations.Add($"schemas.{name} is missing");
            return;
        }
        if (!TryGetString(schema, "schemaId", out string? actualSchemaId)
            || !string.Equals(actualSchemaId, schemaId, StringComparison.Ordinal))
            violations.Add($"schemas.{name}.schemaId is incorrect");
        if (!TryGetString(schema, "apiVersion", out string? actualApiVersion)
            || !string.Equals(actualApiVersion, ApiVersion, StringComparison.Ordinal))
            violations.Add($"schemas.{name}.apiVersion is incorrect");
        if (!TryGetString(schema, "kind", out string? actualKind)
            || !string.Equals(actualKind, kind, StringComparison.Ordinal))
            violations.Add($"schemas.{name}.kind is incorrect");
        if (!schema.TryGetProperty("closedObjects", out JsonElement closed)
            || closed.ValueKind != JsonValueKind.True)
            violations.Add($"schemas.{name}.closedObjects must be true");
    }

    private static void ValidateContractFacts(
        JsonElement root,
        string contractName,
        string propertySetName,
        string[] expectedProperties,
        ICollection<string> violations)
    {
        if (!TryGetNested(root, out JsonElement contract, contractName))
        {
            violations.Add($"{contractName} is missing");
            return;
        }
        if (!contract.TryGetProperty("targetBound", out JsonElement targetBound)
            || targetBound.ValueKind != JsonValueKind.True)
            violations.Add($"{contractName}.targetBound must be true");
        if (!contract.TryGetProperty("valueFree", out JsonElement valueFree)
            || valueFree.ValueKind != JsonValueKind.True)
            violations.Add($"{contractName}.valueFree must be true");
        ValidateExactArray(contract, propertySetName, expectedProperties, violations);
    }

    private static void ValidateExactArray(
        JsonElement owner,
        string propertyName,
        string[] expected,
        ICollection<string> violations)
    {
        if (!owner.TryGetProperty(propertyName, out JsonElement array)
            || array.ValueKind != JsonValueKind.Array)
        {
            violations.Add($"{propertyName} must be an array");
            return;
        }
        string[] actual = array.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .ToArray();
        if (actual.Length != array.GetArrayLength()
            || !actual.Order(StringComparer.Ordinal).SequenceEqual(
                expected.Order(StringComparer.Ordinal), StringComparer.Ordinal))
            violations.Add($"{propertyName} does not match the frozen set");
    }

    private static void ValidateSchema(string repositoryPath, string schemaId, string kind)
    {
        string path = ContextSystemHelpers.RepoPath(
            repositoryPath.Split('/', StringSplitOptions.RemoveEmptyEntries));
        if (!File.Exists(path))
            throw new InvalidOperationException($"Frozen schema is missing: {repositoryPath}");

        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(path));
        JsonElement root = document.RootElement;
        if (!string.Equals(root.GetProperty("$schema").GetString(),
                "https://json-schema.org/draft/2020-12/schema", StringComparison.Ordinal)
            || !string.Equals(root.GetProperty("$id").GetString(), schemaId,
                StringComparison.Ordinal)
            || root.GetProperty("type").GetString() != "object"
            || root.GetProperty("additionalProperties").ValueKind != JsonValueKind.False
            || !string.Equals(root.GetProperty("properties").GetProperty("$schema")
                .GetProperty("const").GetString(), schemaId, StringComparison.Ordinal)
            || !string.Equals(root.GetProperty("properties").GetProperty("apiVersion")
                .GetProperty("const").GetString(), ApiVersion, StringComparison.Ordinal)
            || !string.Equals(root.GetProperty("properties").GetProperty("kind")
                .GetProperty("const").GetString(), kind, StringComparison.Ordinal)
            || !AllTypedObjectsAreClosed(root))
        {
            throw new InvalidOperationException(
                $"Frozen schema identity or closed-object behavior drifted: {repositoryPath}");
        }
    }

    private static bool AllTypedObjectsAreClosed(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("type", out JsonElement type)
                && type.ValueKind == JsonValueKind.String
                && string.Equals(type.GetString(), "object", StringComparison.Ordinal)
                && (!element.TryGetProperty("additionalProperties", out JsonElement additional)
                    || additional.ValueKind != JsonValueKind.False))
                return false;
            return element.EnumerateObject()
                .All(property => AllTypedObjectsAreClosed(property.Value));
        }
        return element.ValueKind != JsonValueKind.Array
            || element.EnumerateArray().All(AllTypedObjectsAreClosed);
    }

    private static IReadOnlyDictionary<string, XDocument> CreateCompliantFixture() =>
        AllowedProjectReferences.ToDictionary(
            pair => pair.Key,
            pair => XDocument.Parse(
                $"""
                <Project><ItemGroup>
                  <ProjectReference Include="../{pair.Value[0]}/{pair.Value[0]}.csproj" />
                  {(pair.Key.EndsWith(".Tests", StringComparison.Ordinal) ? "<PackageReference Include=\"TUnit\" />" : string.Empty)}
                </ItemGroup></Project>
                """),
            StringComparer.Ordinal);

    private static string CreateBaselineFixture(int cardinality, bool valueFree) =>
        $$"""
        {
          "schemaVersion": 1,
          "schemas": {
            "configurationManifest": {"schemaId":"{{ManifestSchemaId}}","apiVersion":"{{ApiVersion}}","kind":"ConfigurationManifest","closedObjects":true},
            "tenantConfigurationPackage": {"schemaId":"{{TenantPackageSchemaId}}","apiVersion":"{{ApiVersion}}","kind":"TenantConfigurationPackage","closedObjects":true}
          },
          "portabilityRegistry": {
            "cardinality": {{cardinality}},
            "keys": [{{string.Join(",", RegistryKeys.Select(value => JsonSerializer.Serialize(value)))}}],
            "excludedAuthorityKeys": [{{string.Join(",", ExcludedRegistryKeys.Select(value => JsonSerializer.Serialize(value)))}}]
          },
          "importSession": {"targetBound":true,"valueFree":{{valueFree.ToString().ToLowerInvariant()}},"targetProperties":["AuthorityKey","Scope","TenantId"]},
          "importPreview": {"targetBound":true,"valueFree":true,"bindingProperties":[{{string.Join(",", BindingProperties.Select(value => JsonSerializer.Serialize(value)))}}]}
        }
        """;

    private static string[] ValidateCiGovernance(string caller, string reusable)
    {
        var violations = new List<string>();
        string[] setupRouteInputs =
        [
            "src/Event.Setup.Core/*", "src/Event.SetupAssistant/*",
            "src/Event.SetupAssistant.Browser/*", "src/Event.SetupAssistant.Desktop/*",
            "src/Event.SetupAssistant.Cli/*", "tests/Event.Setup*.Tests/*",
            "eng/setup-assistant/*", "browser-release-capabilities.json",
            "frozen-contract-baseline.json"
        ];
        foreach (string routeInput in setupRouteInputs)
        {
            if (!caller.Contains(routeInput, StringComparison.Ordinal))
                violations.Add($"missing Setup route input: {routeInput}");
        }

        string[] requiredCallerValues =
        [
            "run-setup-tests: ${{ steps.changes.outputs.run-setup-tests }}",
            "run-setup-tests: ${{ needs.detect-build-test-changes.outputs.run-setup-tests == 'true' }}",
            "run_setup=\"true\"", "run_arch=\"true\"",
            "run-integration-tests: false"
        ];
        foreach (string required in requiredCallerValues)
        {
            if (!caller.Contains(required, StringComparison.Ordinal))
                violations.Add($"missing caller control: {required}");
        }

        string[] setupProjects = RequiredSolutionProjects
            .Where(pair => pair.Key.EndsWith(".Tests", StringComparison.Ordinal))
            .Select(pair => pair.Value)
            .ToArray();
        foreach (string project in setupProjects)
        {
            if (!reusable.Contains(project, StringComparison.Ordinal))
                violations.Add($"missing Setup project gate: {project}");
        }

        if (!reusable.Contains("run-setup-tests:", StringComparison.Ordinal)
            || !reusable.Contains("inputs.run-setup-tests", StringComparison.Ordinal))
            violations.Add("reusable Setup lane input is missing");
        if (!reusable.Contains(
                "dotnet run eng/setup-assistant/GenerateSetupAssistantRatchets.cs -- --check",
                StringComparison.Ordinal)
            || !reusable.Contains(
                "eng/setup-assistant/SetupCliCommandSchemaGenerator/SetupCliCommandSchemaGenerator.csproj",
                StringComparison.Ordinal)
            || !reusable.Contains("SetupCliCommandSchemaGenerator.csproj --configuration \"$DOTNET_CONFIGURATION\" --no-restore -- --check",
                StringComparison.Ordinal)
            || reusable.Contains("GenerateSetupAssistantRatchets.cs -- --write",
                StringComparison.Ordinal)
            || reusable.Contains("SetupCliCommandSchemaGenerator.csproj --configuration \"$DOTNET_CONFIGURATION\" --no-restore -- --write",
                StringComparison.Ordinal))
            violations.Add("ratchet or command-schema check is missing or mutating");

        ValidateWorkflowAuthority(caller, "caller", violations);
        ValidateWorkflowAuthority(reusable, "reusable", violations);
        return [.. violations];
    }

    private static void ValidateWorkflowAuthority(
        string workflow,
        string name,
        List<string> violations)
    {
        if (!workflow.Contains("permissions:\n  contents: read", StringComparison.Ordinal))
            violations.Add($"{name} workflow lacks top-level contents: read");
        string[] forbidden =
        [
            "contents: write", "id-token: write", "packages: write",
            "attestations: write", "secrets: inherit", "${{ secrets.",
            "persist-credentials: true"
        ];
        foreach (string value in forbidden)
        {
            if (workflow.Contains(value, StringComparison.Ordinal))
                violations.Add($"{name} workflow contains forbidden authority: {value}");
        }

        int checkoutCount = CountOccurrences(workflow, "uses: actions/checkout@");
        int safeCheckoutCount = CountOccurrences(workflow, "persist-credentials: false");
        if (checkoutCount != safeCheckoutCount)
            violations.Add($"{name} workflow has a checkout without disabled credential persistence");
    }

    private static int CountOccurrences(string value, string token)
    {
        int count = 0;
        int offset = 0;
        while ((offset = value.IndexOf(token, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += token.Length;
        }
        return count;
    }

    private static bool IsIgnoredByGit(string repositoryPath)
    {
        ProcessResult result = RunProcess(
            "git", ["check-ignore", "--no-index", "--quiet", repositoryPath],
            ContextSystemHelpers.RepoPath());
        return result.ExitCode switch
        {
            0 => true,
            1 => false,
            _ => throw new InvalidOperationException(
                $"git check-ignore failed for {repositoryPath}: {result.Output}")
        };
    }

    private static ProcessResult RunProcess(
        string fileName,
        IEnumerable<string> arguments,
        string workingDirectory)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start {fileName}.");
        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(milliseconds: 30_000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"{fileName} did not finish within 30 seconds.");
        }

        return new ProcessResult(
            process.ExitCode,
            string.Concat(standardOutput, Environment.NewLine, standardError));
    }

    private readonly record struct ProcessResult(int ExitCode, string Output);

    private static bool TryGetNested(
        JsonElement root,
        out JsonElement value,
        params string[] path)
    {
        value = root;
        foreach (string segment in path)
        {
            if (value.ValueKind != JsonValueKind.Object
                || !value.TryGetProperty(segment, out value))
                return false;
        }
        return true;
    }

    private static bool TryGetInt32(
        JsonElement owner,
        string propertyName,
        out int value)
    {
        value = 0;
        return owner.ValueKind == JsonValueKind.Object
            && owner.TryGetProperty(propertyName, out JsonElement property)
            && property.TryGetInt32(out value);
    }

    private static bool TryGetString(
        JsonElement owner,
        string propertyName,
        out string? value)
    {
        value = null;
        return owner.ValueKind == JsonValueKind.Object
            && owner.TryGetProperty(propertyName, out JsonElement property)
            && property.ValueKind == JsonValueKind.String
            && (value = property.GetString()) is not null;
    }

    private static string[] PublicPropertyNames(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static bool IsValueBearingPropertyName(string name) =>
        new[]
        {
            "AccessToken", "ArtifactBytes", "ConnectionString", "Content",
            "FilePath", "Payload", "SecretValue", "SourceTenantId"
        }.Any(forbidden => string.Equals(
            name, forbidden, StringComparison.OrdinalIgnoreCase));

    private static string NormalizePath(string path) => path.Replace('\\', '/');
}
