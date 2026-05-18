// ABOUTME: Architecture guardrails for CQRS handler-to-validator pairing in the Application layer.
// ABOUTME: Preserves the repository convention that handlers manually instantiate validators instead of injecting them.

namespace Event.Architecture.Tests;

using System.Reflection;
using FluentValidation;
using MediatR;

/// <summary>
/// Enforces the local CQRS validation contract without introducing FluentValidation DI.
/// </summary>
public class HandlerValidatorPairingTests
{
    private static readonly Assembly ApplicationAssembly = typeof(Explore.Application.ApplicationServicesRegistration).Assembly;
    private static readonly string SourceRoot = LocateSourceRoot();

    private static readonly IReadOnlyDictionary<string, string> ValidationExemptions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Explore.Application.Features.InstanceOnboarding.Handlers.Commands.UpdateModuleSettingsCommandHandler"] = "Settings validation is delegated to IInstanceGovernanceSettingService; handler performs admin authorization only.",
        ["Explore.Application.Features.InstanceOnboarding.Handlers.Commands.UpdateEventPolicyCommandHandler"] = "Settings validation is delegated to IInstanceGovernanceSettingService; handler performs admin authorization only.",
        ["Explore.Application.Features.InstanceOnboarding.Handlers.Commands.UpdateOrganizationPolicyCommandHandler"] = "Settings validation is delegated to IInstanceGovernanceSettingService; handler performs admin authorization only.",
        ["Explore.Application.Features.InstanceOnboarding.Handlers.Commands.UpdateBrandingSettingsCommandHandler"] = "Settings validation is delegated to IInstanceGovernanceSettingService; handler performs admin authorization only.",
        ["Explore.Application.Features.InstanceOnboarding.Handlers.Commands.UpdateDomainSettingsCommandHandler"] = "Settings validation is delegated to IInstanceGovernanceSettingService; handler performs admin authorization only.",
        ["Explore.Application.Features.InstanceOnboarding.Handlers.Commands.UpdateTenantDelegationSettingsCommandHandler"] = "Settings validation is delegated to IInstanceGovernanceSettingService; handler performs admin authorization only.",
    };

    [Test]
    public async Task CqrsHandlers_WithMatchingValidators_ShouldInstantiateThemManuallyOrBeExplicitlyExempt()
    {
        var validatorTargets = GetValidatorTargets();
        var failures = new List<string>();

        foreach (var handler in GetCqrsHandlers())
        {
            var candidateTargets = GetCandidateValidationTargets(handler.RequestType, validatorTargets.Keys.ToList());
            var candidateValidators = candidateTargets
                .SelectMany(target => validatorTargets[target])
                .DistinctBy(type => type.FullName)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList();

            if (candidateValidators.Count == 0)
            {
                continue;
            }

            var source = ReadHandlerSource(handler.HandlerType);
            var instantiatedValidators = candidateValidators
                .Where(validator => source.Contains($"new {validator.Name}", StringComparison.Ordinal))
                .Select(validator => validator.Name)
                .ToList();

            if (instantiatedValidators.Count > 0)
            {
                continue;
            }

            var handlerName = handler.HandlerType.FullName ?? handler.HandlerType.Name;
            if (ValidationExemptions.TryGetValue(handlerName, out var reason) && !string.IsNullOrWhiteSpace(reason))
            {
                continue;
            }

            failures.Add(
                $"{handlerName} handles {handler.RequestType.FullName} and has candidate validators " +
                $"[{string.Join(", ", candidateValidators.Select(v => v.Name))}], but none are manually instantiated and no exemption explains why.");
        }

        if (failures.Count > 0)
        {
            Console.WriteLine("CQRS handler-validator pairing failures:");
            foreach (var failure in failures)
            {
                Console.WriteLine($"  - {failure}");
            }
        }

        await Assert.That(failures).IsEmpty();
    }

    [Test]
    public async Task CqrsHandlers_ShouldNotInjectFluentValidationValidators()
    {
        var failures = GetCqrsHandlers()
            .SelectMany(handler => handler.HandlerType.GetConstructors().SelectMany(constructor => constructor.GetParameters(), (constructor, parameter) => new
            {
                handler.HandlerType,
                Constructor = constructor,
                Parameter = parameter,
            }))
            .Where(item => IsValidatorType(item.Parameter.ParameterType))
            .Select(item => $"{item.HandlerType.FullName} constructor injects {item.Parameter.ParameterType.FullName} via parameter '{item.Parameter.Name}'.")
            .Order(StringComparer.Ordinal)
            .ToList();

        if (failures.Count > 0)
        {
            Console.WriteLine("CQRS handlers must instantiate validators manually, not through DI:");
            foreach (var failure in failures)
            {
                Console.WriteLine($"  - {failure}");
            }
        }

        await Assert.That(failures).IsEmpty();
    }

    [Test]
    public async Task HandlerValidatorExemptions_ShouldReferenceExistingHandlersAndHaveReasons()
    {
        var handlerNames = GetCqrsHandlers()
            .Select(handler => handler.HandlerType.FullName)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.Ordinal);

        var failures = ValidationExemptions
            .Where(exemption => string.IsNullOrWhiteSpace(exemption.Value) || !handlerNames.Contains(exemption.Key))
            .Select(exemption => string.IsNullOrWhiteSpace(exemption.Value)
                ? $"{exemption.Key} has an empty exemption reason."
                : $"{exemption.Key} is exempted but no matching IRequestHandler type exists.")
            .Order(StringComparer.Ordinal)
            .ToList();

        await Assert.That(failures).IsEmpty();
    }

    private static IReadOnlyList<CqrsHandlerDescriptor> GetCqrsHandlers() => ApplicationAssembly
        .GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false })
        .SelectMany(type => type.GetInterfaces()
            .Where(IsRequestHandlerInterface)
            .Select(handlerInterface => new CqrsHandlerDescriptor(type, handlerInterface.GetGenericArguments()[0])))
        .Where(handler => IsApplicationFeatureRequest(handler.RequestType))
        .DistinctBy(handler => handler.HandlerType.FullName)
        .OrderBy(handler => handler.HandlerType.FullName, StringComparer.Ordinal)
        .ToList();

    private static IReadOnlyDictionary<Type, IReadOnlyList<Type>> GetValidatorTargets() => ApplicationAssembly
        .GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false })
        .Select(type => new
        {
            ValidatorType = type,
            TargetTypes = type.GetInterfaces()
                .Where(IsValidatorInterface)
                .Select(validatorInterface => validatorInterface.GetGenericArguments()[0])
                .Where(IsApplicationValidationTarget)
                .ToList(),
        })
        .Where(item => item.TargetTypes.Count > 0)
        .SelectMany(item => item.TargetTypes.Select(target => new { target, item.ValidatorType }))
        .GroupBy(item => item.target)
        .ToDictionary(
            group => group.Key,
            group => (IReadOnlyList<Type>)group.Select(item => item.ValidatorType)
                .DistinctBy(type => type.FullName)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList());

    private static IReadOnlyList<Type> GetCandidateValidationTargets(Type requestType, IReadOnlyCollection<Type> validatorTargetTypes)
    {
        var targets = new List<Type>();

        if (validatorTargetTypes.Contains(requestType))
        {
            targets.Add(requestType);
        }

        targets.AddRange(requestType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.PropertyType)
            .Where(validatorTargetTypes.Contains));

        return targets
            .DistinctBy(type => type.FullName)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToList();
    }

    private static string ReadHandlerSource(Type handlerType)
    {
        var typeName = handlerType.Name;
        var candidates = Directory.EnumerateFiles(Path.Combine(SourceRoot, "Explore.Application"), "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains($"class {typeName}", StringComparison.Ordinal))
            .ToList();

        return candidates.Count == 1
            ? File.ReadAllText(candidates[0])
            : string.Join(Environment.NewLine, candidates.Select(File.ReadAllText));
    }

    private static string LocateSourceRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Explore.sln")) && Directory.Exists(Path.Combine(directory.FullName, "Explore.Application")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing Explore.sln and Explore.Application.");
    }

    private static bool IsRequestHandlerInterface(Type type) => type.IsGenericType
        && type.GetGenericTypeDefinition() == typeof(IRequestHandler<,>);

    private static bool IsValidatorInterface(Type type) => type.IsGenericType
        && type.GetGenericTypeDefinition() == typeof(IValidator<>);

    private static bool IsValidatorType(Type type) => type.IsGenericType
        ? type.GetGenericTypeDefinition() == typeof(IValidator<>)
        : type.GetInterfaces().Any(IsValidatorInterface);

    private static bool IsApplicationFeatureRequest(Type type) => type.Namespace?.StartsWith("Explore.Application.Features.", StringComparison.Ordinal) == true
        && (type.Name.EndsWith("Command", StringComparison.Ordinal) || type.Name.EndsWith("Query", StringComparison.Ordinal) || type.Name.EndsWith("Request", StringComparison.Ordinal));

    private static bool IsApplicationValidationTarget(Type type) => type.Namespace?.StartsWith("Explore.Application.", StringComparison.Ordinal) == true;

    private sealed record CqrsHandlerDescriptor(Type HandlerType, Type RequestType);
}
