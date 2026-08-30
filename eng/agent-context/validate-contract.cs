// ABOUTME: Validates the machine-readable contribution contract and cold-start benchmark routing.
// ABOUTME: Enforces schema, safe paths, references, test projects, and secrets-authority precedence.
#:package YamlDotNet

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

#pragma warning disable IL3050

if (args is ["--validate-path", var candidate])
{
    if (IsSafeRepositoryPath(candidate))
    {
        Console.WriteLine($"PASS safe repository path: {candidate}");
        return 0;
    }

    Console.Error.WriteLine($"FAIL unsafe repository path: {candidate}");
    return 2;
}

var repositoryRoot = Path.GetFullPath(args.FirstOrDefault(argument => !argument.StartsWith("--", StringComparison.Ordinal)) ?? ".");
var intentOption = Array.IndexOf(args, "--intent");
var selectedIntent = intentOption >= 0 && intentOption + 1 < args.Length ? args[intentOption + 1] : null;
var errors = new List<string>();

try
{
    using var manifest = ReadYaml(Path.Combine(repositoryRoot, ".agents/contract/intents.yaml"));
    using var benchmarks = ReadYaml(Path.Combine(repositoryRoot, ".agents/benchmarks/cold-start-tasks.yaml"));
    using var schema = JsonDocument.Parse(File.ReadAllText(Path.Combine(repositoryRoot, ".agents/contract/schema.json")));

    ValidateSchema(manifest.RootElement, schema.RootElement, schema.RootElement, "$", errors);

    var intents = manifest.RootElement.GetProperty("intents").EnumerateArray()
        .ToDictionary(intent => Text(intent, "id"), StringComparer.Ordinal);
    ValidateIntentReferences(repositoryRoot, intents, selectedIntent, errors);
    ValidateBenchmarks(repositoryRoot, benchmarks.RootElement, intents, errors);
    ValidateGovernanceOwnership(intents, errors);
    ValidateSecretsAuthority(intents, errors);

    if (errors.Count > 0)
    {
        foreach (var error in errors)
        {
            Console.Error.WriteLine($"FAIL {error}");
        }

        return 1;
    }

    var routeOption = Array.IndexOf(args, "--route");
    if (routeOption >= 0)
    {
        if (routeOption + 1 >= args.Length) throw new InvalidOperationException("--route requires a repository-relative path");
        var routePath = args[routeOption + 1];
        if (!IsSafeRepositoryPath(routePath)) throw new InvalidOperationException($"unsafe route path: {routePath}");
        var primaryId = selectedIntent ?? "secrets-authority";
        var primary = intents[primaryId];
        if (!Matches(Strings(primary, "paths_in_scope"), routePath)) throw new InvalidOperationException($"{primaryId} does not authorize {routePath}");
        var secondary = primary.TryGetProperty("routing", out var routeConfig)
            ? routeConfig.GetProperty("secondary").EnumerateArray()
                .Where(route => Matches(Strings(route, "when_paths"), routePath))
                .Select(route => Text(route, "intent"))
                .ToArray()
            : [];
        Console.WriteLine($"ROUTE {routePath} => primary={primaryId}; secondary={(secondary.Length == 0 ? "none" : string.Join(',', secondary))}");
    }

    Console.WriteLine($"PASS contract schema and references: {intents.Count} unique intents; scope={selectedIntent ?? "all"}");
    Console.WriteLine($"PASS benchmark registry: {benchmarks.RootElement.GetProperty("scenarios").GetArrayLength()} unique scenarios");
    Console.WriteLine("PASS governance ownership, secondary reachability, expected route sets, and conflict precedence");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"FAIL contract validation could not complete: {exception.Message}");
    return 1;
}

static JsonDocument ReadYaml(string path)
{
    using var reader = File.OpenText(path);
    var yaml = new DeserializerBuilder()
        .WithAttemptingUnquotedStringTypeDeserialization()
        .Build()
        .Deserialize(reader);
    var json = new SerializerBuilder().JsonCompatible().Build().Serialize(yaml);
    return JsonDocument.Parse(json);
}

static void ValidateSchema(
    JsonElement value,
    JsonElement rule,
    JsonElement schemaRoot,
    string path,
    List<string> errors)
{
    if (rule.TryGetProperty("$ref", out var reference))
    {
        rule = ResolveReference(schemaRoot, reference.GetString()!);
    }

    if (rule.TryGetProperty("type", out var type) && !HasType(value, type.GetString()!))
    {
        errors.Add($"{path} expected {type.GetString()}, found {value.ValueKind}");
        return;
    }

    if (value.ValueKind == JsonValueKind.Object && rule.TryGetProperty("properties", out var properties))
    {
        if (rule.TryGetProperty("required", out var required))
        {
            foreach (var name in required.EnumerateArray().Select(item => item.GetString()!))
            {
                if (!value.TryGetProperty(name, out _)) errors.Add($"{path} missing required property {name}");
            }
        }

        if (rule.TryGetProperty("additionalProperties", out var additional) && additional.ValueKind == JsonValueKind.False)
        {
            foreach (var property in value.EnumerateObject())
            {
                if (!properties.TryGetProperty(property.Name, out _)) errors.Add($"{path} has unknown property {property.Name}");
            }
        }

        foreach (var property in value.EnumerateObject())
        {
            if (properties.TryGetProperty(property.Name, out var propertyRule))
            {
                ValidateSchema(property.Value, propertyRule, schemaRoot, $"{path}.{property.Name}", errors);
            }
        }
    }

    if (value.ValueKind == JsonValueKind.Array)
    {
        if (rule.TryGetProperty("minItems", out var minimumItems) && value.GetArrayLength() < minimumItems.GetInt32())
        {
            errors.Add($"{path} has fewer than {minimumItems.GetInt32()} items");
        }

        if (rule.TryGetProperty("items", out var itemRule))
        {
            var index = 0;
            foreach (var item in value.EnumerateArray())
            {
                ValidateSchema(item, itemRule, schemaRoot, $"{path}[{index++}]", errors);
            }
        }
    }

    if (value.ValueKind == JsonValueKind.String)
    {
        var text = value.GetString()!;
        if (rule.TryGetProperty("minLength", out var minimumLength) && text.Length < minimumLength.GetInt32())
            errors.Add($"{path} is shorter than {minimumLength.GetInt32()} characters");
        if (rule.TryGetProperty("pattern", out var pattern) && !Regex.IsMatch(text, pattern.GetString()!))
            errors.Add($"{path} does not match its schema pattern: {text}");
        if (rule.TryGetProperty("enum", out var choices) && !choices.EnumerateArray().Any(choice => choice.GetString() == text))
            errors.Add($"{path} has unsupported value {text}");
    }

    if (value.ValueKind == JsonValueKind.Number && rule.TryGetProperty("minimum", out var minimum)
        && value.GetDecimal() < minimum.GetDecimal()) errors.Add($"{path} is below {minimum.GetDecimal()}");
}

static void ValidateIntentReferences(
    string root,
    IReadOnlyDictionary<string, JsonElement> intents,
    string? selectedIntent,
    List<string> errors)
{
    if (selectedIntent is not null && !intents.ContainsKey(selectedIntent))
    {
        errors.Add($"requested intent does not exist: {selectedIntent}");
        return;
    }

    foreach (var (id, intent) in intents.Where(item => selectedIntent is null || item.Key == selectedIntent))
    {
        foreach (var path in Strings(intent, "must_read_docs").Concat(Strings(intent, "load_rules")).Concat(Strings(intent, "docs_to_update")))
        {
            ValidatePath(path, $"{id} reference", errors);
            if (!File.Exists(Path.Combine(root, path))) errors.Add($"{id} references missing file {path}");
        }

        foreach (var path in Strings(intent, "paths_in_scope").Concat(Strings(intent, "paths_forbidden")))
            ValidatePath(path, $"{id} route", errors);
        foreach (var skill in Strings(intent, "load_skills"))
            if (!File.Exists(Path.Combine(root, ".agents/skills", skill, "SKILL.md"))) errors.Add($"{id} references missing skill {skill}");
        foreach (var test in Strings(intent, "minimum_tests"))
            if (!Directory.EnumerateFiles(root, $"{test}.csproj", SearchOption.AllDirectories).Any()) errors.Add($"{id} references missing test project {test}");
        foreach (var related in Strings(intent, "related_intents"))
            if (!intents.ContainsKey(related)) errors.Add($"{id} references missing related intent {related}");
    }
}

static void ValidateBenchmarks(
    string root,
    JsonElement benchmarkRoot,
    IReadOnlyDictionary<string, JsonElement> intents,
    List<string> errors)
{
    var scenarios = benchmarkRoot.GetProperty("scenarios").EnumerateArray().ToArray();
    if (scenarios.Select(scenario => Text(scenario, "id")).Distinct(StringComparer.Ordinal).Count() != scenarios.Length)
        errors.Add("benchmark scenario ids are not unique");
    foreach (var scenario in scenarios)
        if (!intents.ContainsKey(Text(scenario, "intent_id"))) errors.Add($"benchmark {Text(scenario, "id")} references a missing intent");

    var secretsScenario = scenarios.Single(scenario => Text(scenario, "id") == "secrets-authority");
    var secretsIntent = intents["secrets-authority"];
    foreach (var path in Strings(secretsScenario, "expected_must_reads"))
    {
        ValidatePath(path, "secrets-authority benchmark read", errors);
        if (!File.Exists(Path.Combine(root, path))) errors.Add($"secrets-authority benchmark references missing file {path}");
    }
    foreach (var path in Strings(secretsScenario, "expected_paths_in_scope"))
        ValidatePath(path, "secrets-authority benchmark route", errors);
    if (!Strings(secretsScenario, "expected_paths_in_scope").SequenceEqual(Strings(secretsIntent, "paths_in_scope")))
        errors.Add("secrets-authority benchmark paths do not exactly mirror the intent");
    if (!Strings(secretsScenario, "expected_verification_commands").SequenceEqual(Strings(secretsIntent, "verification_commands")))
        errors.Add("secrets-authority benchmark commands do not exactly mirror the intent");
}

static void ValidateSecretsAuthority(IReadOnlyDictionary<string, JsonElement> intents, List<string> errors)
{
    var primary = intents["secrets-authority"];
    var routing = primary.GetProperty("routing");
    if (Text(routing, "precedence") != "primary_then_secondary") errors.Add("secrets-authority precedence must be primary_then_secondary");

    var related = Strings(primary, "related_intents").ToHashSet(StringComparer.Ordinal);
    var primaryPatterns = Strings(primary, "paths_in_scope").ToHashSet(StringComparer.Ordinal);
    var secondary = routing.GetProperty("secondary").EnumerateArray().ToArray();
    if (!secondary.Select(route => Text(route, "intent")).ToHashSet(StringComparer.Ordinal).SetEquals(related))
        errors.Add("secrets-authority routing must classify every related intent exactly once");

    foreach (var route in secondary)
    {
        var secondaryId = Text(route, "intent");
        if (!intents.ContainsKey(secondaryId)) errors.Add($"routing references missing secondary intent {secondaryId}");
        foreach (var path in Strings(route, "when_paths"))
        {
            ValidatePath(path, $"{secondaryId} secondary route", errors);
            if (!primaryPatterns.Contains(path)) errors.Add($"{secondaryId} secondary route is unreachable from primary scope: {path}");
        }
    }

    var conflict = routing.GetProperty("conflict_overrides").EnumerateArray().Single();
    if (Text(conflict, "secondary_intent") != "openapi-contract-change"
        || Text(conflict, "secondary_gate") != "backward_compatibility_preserved"
        || Text(conflict, "controlling_gate") != "no_backward_compatibility")
        errors.Add("secrets-authority must explicitly override only the OpenAPI compatibility gate");
    else
    {
        var primaryGates = Strings(primary.GetProperty("criticality"), "safety_gates").ToHashSet(StringComparer.Ordinal);
        var secondaryGates = Strings(intents[Text(conflict, "secondary_intent")].GetProperty("criticality"), "safety_gates").ToHashSet(StringComparer.Ordinal);
        if (!primaryGates.Contains(Text(conflict, "controlling_gate"))) errors.Add("conflict override controlling gate is not a primary safety gate");
        if (!secondaryGates.Contains(Text(conflict, "secondary_gate"))) errors.Add("conflict override secondary gate is not present on the secondary intent");
    }

    var expectedRoutes = new Dictionary<string, string[]>
    {
        ["src/Explore.Secrets/Bootstrap/BootstrapSecretLoader.cs"] = [],
        ["src/Explore.Domain/Secrets/SecretDefinition.cs"] = [],
        ["src/Explore.Persistence/Configurations/Entities/SecretBindingConfiguration.cs"] = ["add-ef-migration"],
        ["src/Explore.Persistence/Migrations/20260830_RemoveInlineSecrets.cs"] = ["add-ef-migration"],
        ["tests/Event.Persistence.IntegrationTests/Database/PrimaryDatabaseProviderBehaviorContractTests.cs"] = ["add-ef-migration"],
        ["src/Explore.Infrastructure/Analytics/AnalyticsConfigResolver.cs"] = [],
        ["src/Explore.Infrastructure/Storage/S3FileStorageProvider.cs"] = [],
        ["src/Explore.Infrastructure/Mail/SmtpConfigResolver.cs"] = [],
        ["src/Explore.Infrastructure/Services/CerbosConfigResolver.cs"] = [],
        ["src/Explore.Secrets/Services/RotationAwareHttpClientFactory.cs"] = [],
        ["tests/Event.Application.UnitTests/Features/ConfigurationManifest/ConfigurationManifestExportQueryTests.cs"] = [],
        ["tests/Explore.Blazor.IntegrationTests/Endpoints/BffConfigurationManifestEndpointsTests.cs"] = [],
        ["src/Explore.API/Controllers/InstanceAuthenticationSettingsController.cs"] = ["add-get-endpoint", "openapi-contract-change"],
        ["src/Explore.API/Hateoas/Policies/InstanceSettingGroupLinkPolicy.cs"] = ["add-hal-link"],
        ["src/Explore.Blazor.Client/Pages/Admin/Instance/InstanceAdminSettings.razor"] = ["blazor-component-affordance"],
        ["docker-compose.yml"] = ["external-infrastructure-bootstrap"],
        ["src/Explore.AppHost/AppHost.cs"] = ["external-infrastructure-bootstrap"],
        ["src/Event.Standalone/Program.cs"] = ["external-infrastructure-bootstrap"],
        ["deploy/bootstrap/README.md"] = ["external-infrastructure-bootstrap"],
        [".github/workflows/_build-test.yml"] = ["ci-cd-change"],
        [".github/workflows/deploy-coolify.yml"] = ["ci-cd-change"]
    };
    foreach (var (path, expected) in expectedRoutes)
    {
        if (!Matches(primaryPatterns, path)) errors.Add($"primary route misses {path}");
        var actual = secondary.Where(route => Matches(Strings(route, "when_paths"), path)).Select(route => Text(route, "intent")).ToArray();
        if (actual.Length != expected.Length || !actual.ToHashSet(StringComparer.Ordinal).SetEquals(expected))
            errors.Add($"secondary route mismatch for {path}: expected [{string.Join(',', expected)}], found [{string.Join(',', actual)}]");
    }

    if (secondary.SelectMany(route => Strings(route, "when_paths")).Contains("docker/**", StringComparer.Ordinal))
        errors.Add("generic docker/** secondary route is not an approved secrets-authority path");
    foreach (var rejected in new[] { "/tmp/secret", "../.env.example", "src/../secret", "C:/secret", @"src\secret", @"C:\secret", @"\\server\share" })
        if (IsSafeRepositoryPath(rejected)) errors.Add($"path guard accepted {rejected}");
}

static void ValidateGovernanceOwnership(IReadOnlyDictionary<string, JsonElement> intents, List<string> errors)
{
    var governance = intents["create-agent-context-skill"];
    var expected = new[]
    {
        ".agents/contract/intents.yaml",
        ".agents/contract/schema.json",
        ".agents/contract/README.md",
        ".agents/benchmarks/cold-start-tasks.yaml",
        ".agents/benchmarks/README.md",
        "docs/GOVERNANCE.md",
        "eng/agent-context/validate-contract.cs",
        "eng/agent-context/packages.lock.json"
    };
    foreach (var path in expected)
        if (!Matches(Strings(governance, "paths_in_scope"), path)) errors.Add($"create-agent-context-skill does not authorize GOV-001 file {path}");
}

static JsonElement ResolveReference(JsonElement root, string reference)
{
    var current = root;
    foreach (var segment in reference[2..].Split('/')) current = current.GetProperty(segment);
    return current;
}

static bool HasType(JsonElement value, string type) => type switch
{
    "object" => value.ValueKind == JsonValueKind.Object,
    "array" => value.ValueKind == JsonValueKind.Array,
    "string" => value.ValueKind == JsonValueKind.String,
    "integer" => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
    _ => true
};

static string Text(JsonElement element, string property) => element.GetProperty(property).GetString()!;

static IEnumerable<string> Strings(JsonElement element, string property) =>
    element.TryGetProperty(property, out var values) ? values.EnumerateArray().Select(value => value.GetString()!) : [];

static bool Matches(IEnumerable<string> patterns, string path)
{
    return patterns.Any(pattern => Regex.IsMatch(path, GlobRegex(pattern), RegexOptions.CultureInvariant));
}

static string GlobRegex(string pattern)
{
    var result = new StringBuilder("^");
    for (var index = 0; index < pattern.Length; index++)
    {
        if (pattern[index] == '*' && index + 1 < pattern.Length && pattern[index + 1] == '*')
        {
            index++;
            if (index + 1 < pattern.Length && pattern[index + 1] == '/')
            {
                index++;
                result.Append("(?:.*/)?");
            }
            else result.Append(".*");
        }
        else if (pattern[index] == '*') result.Append("[^/]*");
        else if (pattern[index] == '?') result.Append("[^/]");
        else result.Append(Regex.Escape(pattern[index].ToString()));
    }

    return result.Append('$').ToString();
}

static void ValidatePath(string path, string owner, List<string> errors)
{
    if (!IsSafeRepositoryPath(path)) errors.Add($"{owner} contains unsafe absolute or traversal path {path}");
}

static bool IsSafeRepositoryPath(string path) =>
    !string.IsNullOrWhiteSpace(path)
    && !Path.IsPathRooted(path)
    && !path.Contains('\\')
    && !Regex.IsMatch(path, "^[A-Za-z]:[/\\\\]")
    && !path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries).Contains("..", StringComparer.Ordinal);
