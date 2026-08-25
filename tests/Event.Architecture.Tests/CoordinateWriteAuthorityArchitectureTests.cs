// ABOUTME: Guards untrusted Application and generated machine request contracts from acquiring raw coordinate authority.
// ABOUTME: Explicitly allowlists governed coordinate reads while structurally discovering writable request graphs.

using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Explore.Application.Contracts.LocationPrivacy;
using Explore.Application.DTOs.Location;
using Explore.Application.Features.Federation.Atproto.Models;
using MediatR;
using GeneratedEventApiClient = Explore.Blazor.Client.Clients.IEventApiClient;

namespace Event.Architecture.Tests;

public sealed class CoordinateWriteAuthorityArchitectureTests
{
    private static readonly Assembly ApplicationAssembly = typeof(CreateLocationDto).Assembly;
    private static readonly Assembly GeneratedClientAssembly = typeof(GeneratedEventApiClient).Assembly;

    private static readonly ImmutableArray<string> ExpectedGeneratedRequestRoots =
    [
        "Explore.Blazor.Client.Clients.CreateEventDraftRequestDto",
        "Explore.Blazor.Client.Clients.CreateLocationDto",
        "Explore.Blazor.Client.Clients.UpdateLocationDto"
    ];

    private static readonly ImmutableArray<string> ExpectedGeneratedRequestGraph =
    [
        "Explore.Blazor.Client.Clients.CreateEventLocationDto"
    ];

    private static readonly Type[] AuthorizedEventLocationDisclosureTypes =
    [
        typeof(EventLocationPublicFieldsDto),
        typeof(EventLocationAttendeeFieldsDto),
        typeof(EventLocationManagementFieldsDto)
    ];

    private static readonly Type[] AuthorizedCoordinateReadTypes =
    [
        .. AuthorizedEventLocationDisclosureTypes,
        typeof(LocationDto),
        typeof(EventLocationDisclosureValues),
        typeof(AtprotoEventLocationSnapshot)
    ];

    private static readonly string[] WriteNamePrefixes =
    [
        "Abandon",
        "Add",
        "Apply",
        "Archive",
        "Assign",
        "Bootstrap",
        "Cancel",
        "Clone",
        "Complete",
        "Configure",
        "Confirm",
        "Create",
        "Decide",
        "Delete",
        "Drain",
        "Execute",
        "Import",
        "Manage",
        "Open",
        "Patch",
        "Pause",
        "Propose",
        "Publish",
        "Purge",
        "Rebuild",
        "Reconcile",
        "Record",
        "Redrive",
        "Remove",
        "Repair",
        "Resume",
        "Review",
        "Rotate",
        "Save",
        "Schedule",
        "Send",
        "Set",
        "Submit",
        "Subscribe",
        "Sync",
        "Transition",
        "Triage",
        "Unsubscribe",
        "Update",
        "Withdraw"
    ];

    [Test]
    public async Task AuthorizedCoordinateReadsAreCompleteAndExcludedFromWriteDiscovery()
    {
        string[] disclosureCoordinates = AuthorizedEventLocationDisclosureTypes
            .SelectMany(FindRawCoordinateMembers)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] coordinateContractTypes = DiscoverApplicationCoordinateContractTypes()
            .Select(GetTypeName)
            .ToArray();
        Type[] writeRoots = DiscoverAllUntrustedWriteContractRoots();
        string[] writeRootTypes = writeRoots
            .Select(GetTypeName)
            .ToArray();
        string[] writeGraphTypes = DiscoverReachableContractTypes(writeRoots)
            .Select(GetTypeName)
            .ToArray();
        string[] authorizedTypeNames = AuthorizedCoordinateReadTypes
            .Select(GetTypeName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        await Assert.That(disclosureCoordinates).IsEquivalentTo(
        [
            $"{GetTypeName(typeof(EventLocationAttendeeFieldsDto))}.Latitude",
            $"{GetTypeName(typeof(EventLocationAttendeeFieldsDto))}.Longitude",
            $"{GetTypeName(typeof(EventLocationManagementFieldsDto))}.Latitude",
            $"{GetTypeName(typeof(EventLocationManagementFieldsDto))}.Longitude",
            $"{GetTypeName(typeof(EventLocationPublicFieldsDto))}.Latitude",
            $"{GetTypeName(typeof(EventLocationPublicFieldsDto))}.Longitude"
        ]);
        await Assert.That(authorizedTypeNames.Except(coordinateContractTypes, StringComparer.Ordinal)).IsEmpty();
        await Assert.That(writeRootTypes.Intersect(authorizedTypeNames, StringComparer.Ordinal)).IsEmpty();
        await Assert.That(writeGraphTypes.Intersect(authorizedTypeNames, StringComparer.Ordinal)).IsEmpty();
    }

    [Test]
    public async Task GeneratedClientRequestDiscoveryUsesApiMethodInputsAndTraversesNestedSchemas()
    {
        string[] generatedRequestRoots = DiscoverGeneratedClientRequestRoots()
            .Select(GetTypeName)
            .ToArray();
        string[] generatedRequestGraph = DiscoverReachableContractTypes(DiscoverGeneratedClientRequestRoots())
            .Select(GetTypeName)
            .ToArray();

        await Assert.That(ExpectedGeneratedRequestRoots.Except(generatedRequestRoots, StringComparer.Ordinal)).IsEmpty();
        await Assert.That(ExpectedGeneratedRequestGraph.Except(generatedRequestGraph, StringComparer.Ordinal)).IsEmpty();
    }

    [Test]
    public async Task UntrustedAndGeneratedWriteContractsMustNotExposeRawCoordinates()
    {
        Type[] writeRoots = DiscoverAllUntrustedWriteContractRoots();
        string[] violations = DiscoverRawCoordinateWriteMembers(writeRoots);

        ReportCoordinateWriteDebt(violations);
        await Assert.That(violations).IsEmpty()
            .Because(
                "raw latitude/longitude members let browser, AI, or generated input manufacture coordinate authority; "
                + "use governed manual address data or a protected provider selection instead. Violations:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    [Test]
    public async Task ScannerDetectsPartialPairsAliasesAndGeneratedRequestsWithoutReadOnlyFalsePositives()
    {
        Type[] candidates =
        [
            typeof(PartialCoordinateWritePayload),
            typeof(AliasedCoordinateWriteRequest),
            typeof(GeneratedCodeCoordinateContractFixture),
            typeof(CompilerGeneratedCoordinateContractFixture),
            typeof(ReadOnlyCoordinateWriteInput)
        ];
        Type[] writeRoots = candidates
            .Where(type => IsUntrustedApplicationWriteContractRoot(type, requireApplicationAssembly: false))
            .OrderBy(GetTypeName, StringComparer.Ordinal)
            .ToArray();
        string[] violations = DiscoverRawCoordinateWriteMembers(writeRoots);

        await Assert.That(writeRoots.Select(GetTypeName)).IsEquivalentTo(candidates.Select(GetTypeName));
        await Assert.That(violations).IsEquivalentTo(
        [
            $"{GetTypeName(typeof(AliasedCoordinateWriteRequest))}.GeneratedValue [json:longitude]",
            $"{GetTypeName(typeof(CompilerGeneratedCoordinateContractFixture))}.Longitude",
            $"{GetTypeName(typeof(GeneratedCodeCoordinateContractFixture))}.Latitude",
            $"{GetTypeName(typeof(PartialCoordinateWritePayload))}.Latitude"
        ]);
    }

    private static Type[] DiscoverAllUntrustedWriteContractRoots() =>
    [
        .. DiscoverUntrustedApplicationWriteContractRoots(),
        .. DiscoverGeneratedClientRequestRoots()
    ];

    private static Type[] DiscoverUntrustedApplicationWriteContractRoots() => ApplicationAssembly
        .GetTypes()
        .Where(type => IsUntrustedApplicationWriteContractRoot(type, requireApplicationAssembly: true))
        .OrderBy(GetTypeName, StringComparer.Ordinal)
        .ToArray();

    private static Type[] DiscoverGeneratedClientRequestRoots() => typeof(GeneratedEventApiClient)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .SelectMany(method => method.GetParameters())
        .Select(parameter => parameter.ParameterType)
        .SelectMany(GetNestedContractTypes)
        .Where(type => type.Assembly == GeneratedClientAssembly)
        .Where(type => type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false })
        .Where(IsGenerated)
        .Distinct()
        .OrderBy(GetTypeName, StringComparer.Ordinal)
        .ToArray();

    private static Type[] DiscoverApplicationCoordinateContractTypes() => ApplicationAssembly
        .GetTypes()
        .Where(type => IsApplicationContractShape(type) || IsAuthorizedCoordinateRead(type))
        .Where(type => FindRawCoordinateMembers(type).Length != 0)
        .OrderBy(GetTypeName, StringComparer.Ordinal)
        .ToArray();

    private static bool IsUntrustedApplicationWriteContractRoot(Type type, bool requireApplicationAssembly)
    {
        if (requireApplicationAssembly && type.Assembly != ApplicationAssembly)
            return false;
        if (type is not { IsClass: true, IsAbstract: false, ContainsGenericParameters: false })
            return false;
        if (IsAuthorizedCoordinateRead(type))
            return false;

        return IsMachineGeneratedRequest(type)
            || IsWriteContractByConvention(type)
            || (IsApplicationContractShape(type) && FindRawCoordinateMembers(type).Length != 0);
    }

    private static bool IsWriteContractByConvention(Type type)
    {
        if (!IsApplicationContractShape(type))
            return false;

        string name = type.Name;
        string typeNamespace = type.Namespace ?? string.Empty;
        if (typeof(IBaseRequest).IsAssignableFrom(type))
        {
            return typeNamespace.Contains(".Requests.Commands", StringComparison.Ordinal)
                || name.EndsWith("Command", StringComparison.Ordinal)
                || IsGenerated(type);
        }

        return HasContractSuffix(name, "Input")
            || HasContractSuffix(name, "Payload")
            || WriteNamePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static bool IsApplicationContractShape(Type type)
    {
        string name = type.Name;
        string typeNamespace = type.Namespace ?? string.Empty;

        return typeof(IBaseRequest).IsAssignableFrom(type)
            || typeNamespace.StartsWith("Explore.Application", StringComparison.Ordinal)
                && !typeNamespace.Contains(".Validators", StringComparison.Ordinal)
                && !name.EndsWith("Validator", StringComparison.Ordinal)
                && (HasContractSuffix(name, "Dto")
                    || HasContractSuffix(name, "Request")
                    || HasContractSuffix(name, "Command")
                    || HasContractSuffix(name, "Input")
                    || HasContractSuffix(name, "Payload"))
            || HasContractSuffix(name, "Request")
            || HasContractSuffix(name, "Command")
            || HasContractSuffix(name, "Input")
            || HasContractSuffix(name, "Payload");
    }

    private static bool HasContractSuffix(string name, string suffix) =>
        name.EndsWith(suffix, StringComparison.Ordinal)
        || name.EndsWith($"{suffix}Dto", StringComparison.Ordinal);

    private static bool IsMachineGeneratedRequest(Type type) =>
        typeof(IBaseRequest).IsAssignableFrom(type) && IsGenerated(type);

    private static bool IsGenerated(Type type) =>
        type.GetCustomAttribute<GeneratedCodeAttribute>() is not null
        || type.GetCustomAttribute<CompilerGeneratedAttribute>() is not null;

    private static bool IsAuthorizedCoordinateRead(Type type) =>
        AuthorizedCoordinateReadTypes.Contains(type);

    private static string[] DiscoverRawCoordinateWriteMembers(IEnumerable<Type> roots) =>
        DiscoverReachableContractTypes(roots)
            .SelectMany(FindRawCoordinateMembers)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static Type[] DiscoverReachableContractTypes(IEnumerable<Type> roots)
    {
        var pending = new Queue<Type>(roots.OrderBy(GetTypeName, StringComparer.Ordinal));
        var visited = new HashSet<Type>();

        while (pending.TryDequeue(out Type? candidate))
        {
            Type type = UnwrapContractType(candidate);
            if (IsAuthorizedCoordinateRead(type) || !visited.Add(type))
                continue;

            foreach (Type referencedType in PublicInstanceMembers(type)
                         .Select(GetMemberType)
                         .SelectMany(GetNestedContractTypes)
                         .Where(IsReachableContractType)
                         .Where(type => !IsAuthorizedCoordinateRead(type))
                         .OrderBy(GetTypeName, StringComparer.Ordinal))
            {
                pending.Enqueue(referencedType);
            }
        }

        return visited.OrderBy(GetTypeName, StringComparer.Ordinal).ToArray();
    }

    private static string[] FindRawCoordinateMembers(Type type) => PublicInstanceMembers(type)
        .Where(IsPubliclyWritable)
        .Select(member => new
        {
            Member = member,
            JsonName = member.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
        })
        .Where(candidate => IsRawCoordinateName(candidate.Member.Name)
            || IsRawCoordinateName(candidate.JsonName))
        .Select(candidate => candidate.JsonName is { } jsonName
            && !string.Equals(candidate.Member.Name, jsonName, StringComparison.OrdinalIgnoreCase)
                ? $"{GetTypeName(type)}.{candidate.Member.Name} [json:{jsonName}]"
                : $"{GetTypeName(type)}.{candidate.Member.Name}")
        .Order(StringComparer.Ordinal)
        .ToArray();

    private static IEnumerable<MemberInfo> PublicInstanceMembers(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Cast<MemberInfo>()
            .Concat(type.GetFields(BindingFlags.Public | BindingFlags.Instance));

    private static bool IsPubliclyWritable(MemberInfo member) => member switch
    {
        PropertyInfo property => property.SetMethod?.IsPublic == true,
        FieldInfo field => !field.IsInitOnly && !field.IsLiteral,
        _ => false
    };

    private static Type GetMemberType(MemberInfo member) => member switch
    {
        PropertyInfo property => property.PropertyType,
        FieldInfo field => field.FieldType,
        _ => throw new ArgumentOutOfRangeException(nameof(member))
    };

    private static IEnumerable<Type> GetNestedContractTypes(Type type)
    {
        Type unwrapped = UnwrapContractType(type);
        yield return unwrapped;

        if (!unwrapped.IsGenericType)
            yield break;

        foreach (Type argument in unwrapped.GetGenericArguments())
        {
            foreach (Type nestedType in GetNestedContractTypes(argument))
                yield return nestedType;
        }
    }

    private static Type UnwrapContractType(Type type) =>
        Nullable.GetUnderlyingType(type) ?? (type.IsArray ? type.GetElementType()! : type);

    private static bool IsReachableContractType(Type type) =>
        type.Assembly == ApplicationAssembly || type.Assembly == GeneratedClientAssembly;

    private static bool IsRawCoordinateName(string? name) =>
        string.Equals(name, "latitude", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "longitude", StringComparison.OrdinalIgnoreCase);

    private static string GetTypeName(Type type) => type.FullName ?? type.Name;

    private static void ReportCoordinateWriteDebt(string[] violations)
    {
        if (violations.Length == 0)
            return;

        Console.WriteLine($"Coordinate write authority RED - raw coordinate write members ({violations.Length}):");
        foreach (string violation in violations)
            Console.WriteLine($"  - {violation}");
    }

    private sealed class PartialCoordinateWritePayload
    {
        public double? Latitude { get; init; }
    }

    private sealed class AliasedCoordinateWriteRequest
    {
        [JsonPropertyName("longitude")]
        public double? GeneratedValue { get; init; }
    }

    [GeneratedCode("CoordinateWriteAuthorityArchitectureTests", "1.0")]
    private sealed class GeneratedCodeCoordinateContractFixture : IRequest
    {
        public double? Latitude { get; init; }
    }

    [CompilerGenerated]
    private sealed class CompilerGeneratedCoordinateContractFixture : IRequest
    {
        public double? Longitude { get; init; }
    }

    private sealed class ReadOnlyCoordinateWriteInput
    {
        public double? Longitude => 4.3517;
    }
}
