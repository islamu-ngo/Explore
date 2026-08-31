// ABOUTME: Verifies Setup Core purity, immutable ownership, and deterministic workflow behavior through public seams.
// ABOUTME: Exercises synthetic violations to prove compiled metadata and IL ratchets fail closed.

namespace ISLAMU.Setup.Core.Tests;

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using ISLAMU.Event.Setup.Core;
using ISLAMU.Wire.Contracts.ConfigurationPortability;

public sealed class SetupCoreArchitectureTests
{
    private static readonly Assembly ProductAssembly = typeof(SetupProfile).Assembly;

    [Test]
    public async Task ProductAssemblyRemainsWireAndBclOnlyWithoutAmbientAuthority()
    {
        string[] violations = SetupCoreAssemblyVerifier.VerifyAssembly(ProductAssembly);

        await Assert.That(violations).IsEmpty()
            .Because(string.Join("; ", violations));
    }

    [Test]
    public async Task OfflinePortabilityClosureHasNoDotenvOrLiveTargetCapability()
    {
        string[] violations = SetupCoreAssemblyVerifier.VerifyOfflinePortabilityClosure(ProductAssembly);

        await Assert.That(violations).IsEmpty()
            .Because(string.Join("; ", violations));
    }

    [Test]
    public async Task CompiledVerifierRejectsAmbientCallsMutableCollectionsAndLeakingDiagnostics()
    {
        string[] ambient = SetupCoreAssemblyVerifier.VerifyTypes([typeof(SyntheticAmbientFixture)]);
        string[] mutable = SetupCoreAssemblyVerifier.VerifyTypes([typeof(SyntheticMutableFixture)]);
        string[] leaking = SetupCoreAssemblyVerifier.VerifyTypes([typeof(SyntheticLeakingDiagnostic)]);

        await Assert.That(ambient.Any(item => item.Contains("UtcNow", StringComparison.Ordinal))).IsTrue();
        await Assert.That(mutable.Any(item => item.Contains("mutable collection", StringComparison.Ordinal))).IsTrue();
        await Assert.That(leaking.Any(item => item.Contains("diagnostic field", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task PublicCollectionInputsAreSnapshottedAndInvariantlyOrdered()
    {
        var capabilities = new List<SetupCapabilityKey>
        {
            new("legal"), new("configuration"), new("legal")
        };
        var topology = new List<SetupTopologyKey> { new("split"), new("single") };
        var sections = new List<PortableSectionKey>
        {
            new("tenant.settings"), new("instance.documents")
        };
        var profile = new SetupProfile(new SetupProfileIdentity("offline"), capabilities, topology);
        var selection = new SetupSelection(
            SetupScope.Instance,
            ConfigurationImportApplyMode.ApplySelected,
            sections);

        capabilities.Clear();
        topology.Add(new SetupTopologyKey("added"));
        sections[0] = new PortableSectionKey("extensions");

        await Assert.That(string.Join(",", profile.Capabilities.Select(item => item.Value)))
            .IsEqualTo("configuration,legal");
        await Assert.That(string.Join(",", profile.Topology.Select(item => item.Value)))
            .IsEqualTo("single,split");
        await Assert.That(string.Join(",", selection.Sections.Select(item => item.Value)))
            .IsEqualTo("instance.documents,tenant.settings");
        await Assert.That(((IList)profile.Capabilities).IsReadOnly).IsTrue();
    }

    [Test]
    public async Task DigestEqualityFormattingAndOrderingAreCultureIndependentAndRepeatable()
    {
        const string expected = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            foreach (string cultureName in new[] { "tr-TR", "ar-SA" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
                ArtifactDigest computed = ArtifactDigest.Compute("abc"u8);
                ArtifactDigest parsed = ArtifactDigest.Parse(expected.ToUpperInvariant());
                var keys = new[]
                {
                    new PortableSectionKey("tenant.settings"),
                    new PortableSectionKey("instance.settings")
                }.Order().Select(item => item.Value).ToArray();

                await Assert.That(computed.ToString()).IsEqualTo(expected);
                await Assert.That(parsed).IsEqualTo(computed);
                await Assert.That(string.Join(",", keys))
                    .IsEqualTo("instance.settings,tenant.settings");
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Test]
    public async Task ReadinessDerivesReadyIncompleteAndBlockedWithoutValues()
    {
        PortableSectionKey instance = new("instance.settings");
        PortableSectionKey tenant = new("tenant.settings");
        SetupReadinessResult ready = SetupReadiness.Evaluate(
            new SetupReadinessInput([instance], [instance, tenant], []));
        SetupReadinessResult incomplete = SetupReadiness.Evaluate(
            new SetupReadinessInput([tenant, instance], [tenant], []));
        SetupReadinessResult blocked = SetupReadiness.Evaluate(
            new SetupReadinessInput([instance], [instance], [tenant]));

        await Assert.That(ready.State).IsEqualTo(SetupReadinessState.Ready);
        await Assert.That(incomplete.State).IsEqualTo(SetupReadinessState.Incomplete);
        await Assert.That(incomplete.Missing.Select(item => item.Value))
            .IsEquivalentTo(["instance.settings"]);
        await Assert.That(blocked.State).IsEqualTo(SetupReadinessState.Blocked);
        await Assert.That(blocked.Blocked.Select(item => item.Value))
            .IsEquivalentTo(["tenant.settings"]);
    }

    [Test]
    public async Task DiffAndCoverageUseOnlyOrderedSectionKeysAndDigests()
    {
        PortableSectionKey a = new("instance.documents");
        PortableSectionKey b = new("instance.settings");
        PortableSectionKey c = new("tenant.settings");
        ArtifactDigest first = ArtifactDigest.Compute("first"u8);
        ArtifactDigest second = ArtifactDigest.Compute("second"u8);
        var baseline = new Dictionary<PortableSectionKey, ArtifactDigest>
        {
            [b] = first, [a] = first
        };
        var candidate = new Dictionary<PortableSectionKey, ArtifactDigest>
        {
            [c] = second, [b] = second
        };
        SetupDiffInput input = new(baseline, candidate);
        SetupCoverageInput coverageInput = new([c, a, b], [b, a]);

        baseline.Clear();
        candidate.Clear();
        SetupDiffResult diff = SetupDiff.Compare(input);
        SetupCoverageResult coverage = SetupCoverage.Calculate(coverageInput);

        await Assert.That(diff.Added.Select(item => item.Value)).IsEquivalentTo(["tenant.settings"]);
        await Assert.That(diff.Removed.Select(item => item.Value)).IsEquivalentTo(["instance.documents"]);
        await Assert.That(diff.Changed.Select(item => item.Value)).IsEquivalentTo(["instance.settings"]);
        await Assert.That(diff.Unchanged).IsEmpty();
        await Assert.That(coverage.Covered.Select(item => item.Value))
            .IsEquivalentTo(["instance.documents", "instance.settings"]);
        await Assert.That(coverage.Missing.Select(item => item.Value))
            .IsEquivalentTo(["tenant.settings"]);
        await Assert.That(coverage.IsComplete).IsFalse();
    }

    [Test]
    public async Task WorkflowTransitionsAreExplicitRepeatableAndInvalidTransitionsAreValueSafe()
    {
        SetupTransitionResult validated = SetupWorkflow.Transition(
            SetupWorkflowState.Draft, SetupWorkflowAction.Validate);
        SetupTransitionResult ready = SetupWorkflow.Transition(
            validated.State, SetupWorkflowAction.MarkReady);
        SetupTransitionResult exported = SetupWorkflow.Transition(
            ready.State, SetupWorkflowAction.Export);
        SetupTransitionResult invalid = SetupWorkflow.Transition(
            SetupWorkflowState.Draft, SetupWorkflowAction.Export);
        SetupTransitionResult repeated = SetupWorkflow.Transition(
            SetupWorkflowState.Draft, SetupWorkflowAction.Export);

        await Assert.That(exported.State).IsEqualTo(SetupWorkflowState.Exported);
        await Assert.That(exported.Succeeded).IsTrue();
        await Assert.That(invalid.Succeeded).IsFalse();
        await Assert.That(invalid.State).IsEqualTo(SetupWorkflowState.Draft);
        await Assert.That(invalid.Diagnostic).IsEqualTo(repeated.Diagnostic);
        await Assert.That(invalid.Diagnostic!.Code.Value).IsEqualTo("invalid-transition");
        await Assert.That(invalid.Diagnostic.Path.Value).IsEqualTo("$.workflow.state");
        await Assert.That(invalid.Diagnostic.GetType().GetProperties().Select(property => property.Name))
            .IsEquivalentTo(["Code", "Path", "Severity"]);
    }
}

internal static class SetupCoreAssemblyVerifier
{
    private static readonly string[] ForbiddenPublicFragments =
    [
        "Password", "Secret", "Pii", "Credential", "ConnectionString",
        "Email", "UserId", "TenantId", "Provider", "DeploymentCoordinate",
        "EnvironmentValue", "LiveAuthority", "Message", "SuppliedValue"
    ];

    private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode))
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(opCode => opCode.Value);

    internal static string[] VerifyAssembly(Assembly assembly)
    {
        var failures = new List<string>();
        failures.AddRange(assembly.GetReferencedAssemblies()
            .Where(reference => reference.Name is not null
                && !reference.Name.StartsWith("System", StringComparison.Ordinal)
                && reference.Name != "netstandard"
                && reference.Name != "Event.Wire.Contracts")
            .Select(reference => $"forbidden assembly reference {reference.Name}"));
        failures.AddRange(VerifyTypes(assembly.GetTypes()));
        return failures.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }

    internal static string[] VerifyOfflinePortabilityClosure(Assembly assembly)
    {
        Type[] types = assembly.GetExportedTypes().Where(type =>
            type.Name.StartsWith("OfflinePortability", StringComparison.Ordinal)
            || type.Name.StartsWith("OfflineLegal", StringComparison.Ordinal)).ToArray();
        string[] forbiddenCapabilities =
        [
            "Dotenv", "EnvironmentValue", "Secret", "Credential", "Provider",
            "Deployment", "Topology", "Target", "Mapping", "Live", "Apply",
            "Publish", "Schedule", "Accept", "Consent", "UserId", "TenantId"
        ];
        var failures = new List<string>();
        foreach (Type type in types)
        {
            foreach (MethodInfo method in type.GetMethods(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (method.Name != "get_RequiresFreshAcceptance"
                    && forbiddenCapabilities.Any(fragment =>
                        method.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                    failures.Add($"offline authority method {type.FullName}.{method.Name}");
                foreach (Type dependency in method.GetParameters().Select(parameter => parameter.ParameterType)
                             .Append(method.ReturnType))
                    if (dependency.FullName?.Contains("Dotenv", StringComparison.OrdinalIgnoreCase) == true)
                        failures.Add($"offline dotenv dependency {type.FullName}.{method.Name}");
            }

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.PropertyType.FullName?.Contains("Dotenv", StringComparison.OrdinalIgnoreCase) == true)
                    failures.Add($"offline dotenv property {type.FullName}.{property.Name}");
                if (property.Name != "RequiresFreshAcceptance"
                    && forbiddenCapabilities.Any(fragment =>
                        property.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                    failures.Add($"offline authority property {type.FullName}.{property.Name}");
            }
        }

        return failures.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }

    internal static string[] VerifyTypes(IEnumerable<Type> types)
    {
        Type[] closure = types.ToArray();
        var failures = new List<string>();
        foreach (Type type in closure)
        {
            bool isEnvironmentContract = string.Equals(
                type.Namespace, "ISLAMU.Event.Setup.Core.Environment", StringComparison.Ordinal);
            if ((type.IsPublic || type.IsNestedPublic)
                && type.Assembly == typeof(SetupProfile).Assembly
                && type.Namespace != "ISLAMU.Event.Setup.Core"
                && !isEnvironmentContract)
                failures.Add($"forbidden public namespace {type.FullName}");

            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
                    && !field.IsLiteral
                    && !field.IsInitOnly)
                    failures.Add($"writable static field {type.FullName}.{field.Name}");
            }

            if ((type.IsPublic || type.IsNestedPublic) && HasForbiddenPublicName(type.Name)
                && !(isEnvironmentContract && IsApprovedEnvironmentName(type.Name)))
                failures.Add($"forbidden public type claim {type.FullName}");

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsMutableCollection(property.PropertyType))
                    failures.Add($"mutable collection {type.FullName}.{property.Name}");
                if (HasForbiddenPublicName(property.Name)
                    && !(isEnvironmentContract && IsApprovedEnvironmentName(property.Name)))
                    failures.Add($"forbidden public member claim {type.FullName}.{property.Name}");
                if (IsForbiddenDependency(property.PropertyType))
                    failures.Add($"forbidden public dependency {type.FullName}.{property.Name}");
                if (type.Name.EndsWith("Diagnostic", StringComparison.Ordinal)
                    && (isEnvironmentContract
                        ? property.Name is not "Code" and not "Path" and not "Key" and not "Category"
                        : property.Name is not "Code" and not "Path" and not "Severity"))
                    failures.Add($"value-bearing diagnostic field {type.FullName}.{property.Name}");
            }

            foreach (MethodInfo method in type.GetMethods(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if ((HasForbiddenPublicName(method.Name)
                        && !(isEnvironmentContract && IsApprovedEnvironmentName(method.Name)))
                    || method.GetParameters().Any(parameter =>
                        HasForbiddenPublicName(parameter.Name ?? string.Empty)
                        && !(isEnvironmentContract && IsApprovedEnvironmentName(parameter.Name ?? string.Empty))))
                    failures.Add($"forbidden public method claim {type.FullName}.{method.Name}");
                if (IsForbiddenDependency(method.ReturnType)
                    || method.GetParameters().Any(parameter => IsForbiddenDependency(parameter.ParameterType)))
                    failures.Add($"forbidden public method dependency {type.FullName}.{method.Name}");
            }

            foreach (MethodBase method in ImplementationBodies(type))
            {
                foreach (MethodBase called in CalledMethods(method).Where(IsAmbientCall))
                {
                    if (!IsApprovedCryptographicCall(type, called))
                        failures.Add($"ambient call {type.FullName}.{method.Name} -> {called.DeclaringType?.FullName}.{called.Name}");
                }
            }
        }

        return failures.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }

    private static bool HasForbiddenPublicName(string name) =>
        ForbiddenPublicFragments.Any(fragment =>
            name.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static bool IsApprovedEnvironmentName(string name) => name is
        "Provider" or "Providers" or "get_Providers"
        or "SecretBindingEnvironmentKeys" or "get_SecretBindingEnvironmentKeys"
        or "secretBindingEnvironmentKeys" or "IsSecret" or "get_IsSecret" or "isSecret"
        or "Value" or "get_Value" or "value" or "CopyValue"
        or "GeneratedDotenvValue" or "LocalSecretGenerationProfile"
        or "LocalSecretGenerator" or "LocalSecretGenerationResult"
        or "ComposeNoSecrets" or "ComposeWithSecrets";

    private static bool IsForbiddenDependency(Type type)
    {
        type = type.IsByRef ? type.GetElementType()! : type;
        string typeName = type.FullName ?? string.Empty;
        string typeNamespace = type.Namespace ?? string.Empty;
        return typeNamespace.StartsWith("System.IO", StringComparison.Ordinal)
            || typeNamespace.StartsWith("System.Net", StringComparison.Ordinal)
            || typeName == "System.Diagnostics.Process"
            || typeName.StartsWith("Microsoft.Extensions", StringComparison.Ordinal)
            || typeName.StartsWith("System.Reflection", StringComparison.Ordinal);
    }

    private static bool IsMutableCollection(Type type)
    {
        if (type == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(type))
            return false;
        if (type.IsArray || typeof(IList).IsAssignableFrom(type) || typeof(IDictionary).IsAssignableFrom(type))
            return true;
        if (!type.IsGenericType)
            return false;
        Type definition = type.GetGenericTypeDefinition();
        return definition == typeof(List<>)
            || definition == typeof(Dictionary<,>)
            || definition == typeof(HashSet<>)
            || definition == typeof(ICollection<>)
            || definition == typeof(IList<>)
            || definition == typeof(IDictionary<,>)
            || definition == typeof(ISet<>);
    }

    private static IEnumerable<MethodBase> ImplementationBodies(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        return type.GetMethods(flags).Cast<MethodBase>()
            .Concat(type.GetConstructors(flags))
            .Where(method => method.GetMethodBody() is not null);
    }

    private static IEnumerable<MethodBase> CalledMethods(MethodBase method)
    {
        byte[] il = method.GetMethodBody()?.GetILAsByteArray() ?? [];
        for (int index = 0; index < il.Length;)
        {
            OpCode opCode;
            byte first = il[index++];
            short value = first == 0xfe
                ? (short)(0xfe00 | il[index++])
                : first;
            if (!OpCodesByValue.TryGetValue(value, out opCode))
                yield break;

            int operandStart = index;
            int operandSize = OperandSize(opCode.OperandType, il, operandStart);
            if (opCode.OperandType == OperandType.InlineMethod && operandStart + 4 <= il.Length)
            {
                int token = BitConverter.ToInt32(il, operandStart);
                MethodBase? called = null;
                try
                {
                    called = method.Module.ResolveMethod(
                        token,
                        method.DeclaringType?.GetGenericArguments(),
                        method.IsGenericMethod ? method.GetGenericArguments() : null);
                }
                catch (ArgumentException)
                {
                }

                if (called is not null)
                    yield return called;
            }

            index = operandStart + operandSize;
        }
    }

    private static int OperandSize(OperandType operandType, byte[] il, int index) => operandType switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineI
            or OperandType.InlineMethod or OperandType.InlineSig or OperandType.InlineString
            or OperandType.InlineTok or OperandType.InlineType or OperandType.ShortInlineR => 4,
        OperandType.InlineI8 or OperandType.InlineR => 8,
        OperandType.InlineSwitch => index + 4 <= il.Length
            ? 4 + BitConverter.ToInt32(il, index) * 4
            : il.Length - index,
        _ => 0
    };

    private static bool IsAmbientCall(MethodBase method)
    {
        Type? owner = method.DeclaringType;
        string ownerName = owner?.FullName ?? string.Empty;
        string ownerNamespace = owner?.Namespace ?? string.Empty;
        return owner == typeof(Environment)
            || owner == typeof(Random)
            || owner == typeof(Guid) && method.Name == nameof(Guid.NewGuid)
            || owner == typeof(DateTime) && method.Name is "get_Now" or "get_UtcNow" or "get_Today"
            || owner == typeof(DateTimeOffset) && method.Name is "get_Now" or "get_UtcNow"
            || ownerName == "System.Threading.Thread" && method.Name == "Sleep"
            || ownerName == "System.Threading.Tasks.Task" && method.Name == "Delay"
            || ownerNamespace.StartsWith("System.IO", StringComparison.Ordinal)
            || ownerNamespace.StartsWith("System.Net", StringComparison.Ordinal)
            || owner == typeof(System.Security.Cryptography.RandomNumberGenerator)
            || owner?.IsSubclassOf(typeof(System.Security.Cryptography.RandomNumberGenerator)) == true
            || ownerName == "System.Diagnostics.Process";
    }

    private static bool IsApprovedCryptographicCall(Type caller, MethodBase called) =>
        caller.FullName == "ISLAMU.Event.Setup.Core.Environment.LocalSecretGenerator"
        && (called.DeclaringType == typeof(System.Security.Cryptography.RandomNumberGenerator)
            || called.DeclaringType?.IsSubclassOf(
                typeof(System.Security.Cryptography.RandomNumberGenerator)) == true);
}

internal sealed class SyntheticAmbientFixture
{
    public DateTime Read() => DateTime.UtcNow;
}

internal sealed class SyntheticMutableFixture
{
    public List<string> Items { get; } = [];
}

internal sealed record SyntheticLeakingDiagnostic(string Code, string Path, string Severity, string SuppliedValue);
