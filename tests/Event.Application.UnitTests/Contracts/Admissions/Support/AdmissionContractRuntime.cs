// ABOUTME: Constructs and invokes the one explicit Phase 20 Application admission contract while production is absent.
// ABOUTME: Contains no fallback service, method, property, or port-name resolution.

using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Services.Registration;
using Explore.Domain;

namespace ApplicationUnitTests.Contracts.Admissions.Support;

internal static class AdmissionContractRuntime
{
    private static readonly Assembly ApplicationAssembly = typeof(AdmissionIssuanceService).Assembly;
    private static readonly Assembly DomainAssembly = typeof(RegistrationOrder).Assembly;

    internal static Type ApplicationType(string name) => ApplicationAssembly.GetExportedTypes()
        .SingleOrDefault(type => type.Name == name)
        ?? throw Missing($"executable public Application type {name}");

    internal static Type DomainType(string name) => DomainAssembly.GetExportedTypes()
        .SingleOrDefault(type => type.Name == name)
        ?? throw Missing($"canonical Domain type {name}");

    internal static object ApplicationObject(string name, params (string Name, object? Value)[] values) =>
        Create(ApplicationType(name), values);

    internal static object Create(Type type, params (string Name, object? Value)[] values)
    {
        Dictionary<string, object?> source = values.ToDictionary(
            value => value.Name, value => value.Value, StringComparer.OrdinalIgnoreCase);
        foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
                     .OrderByDescending(value => value.GetParameters().Length))
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            if (parameters.Any(parameter =>
                    !source.ContainsKey(parameter.Name!) &&
                    !parameter.HasDefaultValue))
            {
                continue;
            }

            object?[] arguments = parameters.Select(parameter => source.TryGetValue(parameter.Name!, out object? value)
                ? ConvertValue(value, parameter.ParameterType)
                : parameter.DefaultValue).ToArray();
            object instance = constructor.Invoke(arguments);
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         .Where(property => property.SetMethod is not null && source.ContainsKey(property.Name)))
            {
                property.SetValue(instance, ConvertValue(source[property.Name], property.PropertyType));
            }
            return instance;
        }

        throw Missing($"constructible exact contract {type.Name}");
    }

    internal static object Service(
        string serviceName,
        TimeProvider clock,
        IUnitOfWork unitOfWork,
        params (string PortName, object Fake)[] ports)
    {
        Type serviceType = ApplicationType(serviceName);
        Dictionary<string, object> dependencies = ports.ToDictionary(
            port => port.PortName, port => port.Fake, StringComparer.Ordinal);
        var expectedDependencies = dependencies.Keys
            .Append(nameof(TimeProvider))
            .Append(nameof(IUnitOfWork))
            .ToHashSet(StringComparer.Ordinal);
        ConstructorInfo constructor = ResolveServiceConstructor(serviceType, expectedDependencies);
        object?[] arguments = constructor.GetParameters().Select(parameter =>
            parameter.ParameterType == typeof(TimeProvider) ? clock :
            parameter.ParameterType == typeof(IUnitOfWork) ? unitOfWork :
            dependencies[parameter.ParameterType.Name]).ToArray();
        return constructor.Invoke(arguments);
    }

    internal static async Task<object> InvokeAsync(
        object service,
        string methodName,
        object request,
        CancellationToken cancellationToken)
    {
        MethodInfo method = service.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .SingleOrDefault(candidate =>
                candidate.Name == methodName &&
                candidate.GetParameters() is { Length: 2 } parameters &&
                parameters[0].ParameterType == request.GetType() &&
                parameters[1].ParameterType == typeof(CancellationToken))
            ?? throw Missing($"exact method {service.GetType().Name}.{methodName}");
        try
        {
            object? invocation = method.Invoke(service, [request, cancellationToken]);
            return await AwaitResult(invocation, method.ReturnType);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    internal static object ExactObject(object value, string expectedTypeName) => value.GetType().Name == expectedTypeName
        ? value
        : throw Missing($"exact {expectedTypeName} request");

    internal static T Value<T>(object owner, string propertyName)
    {
        PropertyInfo property = owner.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw Missing($"exact property {owner.GetType().Name}.{propertyName}");
        return (T)(property.GetValue(owner)
            ?? throw Missing($"value {owner.GetType().Name}.{propertyName}"));
    }

    internal static object[] Items(object owner, string propertyName) =>
        ((IEnumerable)Value<object>(owner, propertyName)).Cast<object>().ToArray();

    internal static Guid[] Ids(object owner, string propertyName) => Items(owner, propertyName)
        .Select(value => value is Guid id ? id : Value<Guid>(value, "AdmissionTicketId"))
        .ToArray();

    internal static Guid EntityId(object entity) => Value<Guid>(entity, "Id");

    internal static string Outcome(object result) => Value<object>(result, "Outcome").ToString()!;

    internal static Dictionary<string, object?> PublicScalarSnapshot(object result) => result.GetType()
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(property => property.PropertyType.IsPrimitive || property.PropertyType.IsEnum ||
                           property.PropertyType == typeof(string))
        .ToDictionary(property => property.Name, property => property.GetValue(result), StringComparer.Ordinal);

    internal static Type? AsyncPayload(Type returnType)
    {
        if (returnType == typeof(Task) || returnType == typeof(ValueTask) || returnType == typeof(void)) return null;
        return returnType.IsGenericType && (returnType.GetGenericTypeDefinition() == typeof(Task<>) ||
                                            returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            ? returnType.GetGenericArguments()[0]
            : returnType;
    }

    internal static object? WrapAsync(Type returnType, object? payload)
    {
        if (returnType == typeof(void)) return null;
        if (returnType == typeof(Task)) return Task.CompletedTask;
        if (returnType == typeof(ValueTask)) return ValueTask.CompletedTask;
        if (!returnType.IsGenericType) return payload;
        Type payloadType = returnType.GetGenericArguments()[0];
        if (returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(payloadType)
                .Invoke(null, [payload]);
        }
        if (returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            return Activator.CreateInstance(returnType, payload);
        }
        return payload;
    }

    internal static Array TypedArray(Type itemType, IEnumerable<object> values)
    {
        object[] source = values.ToArray();
        Array result = Array.CreateInstance(itemType, source.Length);
        for (int index = 0; index < source.Length; index++) result.SetValue(source[index], index);
        return result;
    }

    internal static Type CollectionItem(Type collectionType) => collectionType.IsArray
        ? collectionType.GetElementType()!
        : collectionType.GetInterfaces().Append(collectionType)
            .Single(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .GetGenericArguments()[0];

    internal static IEnumerable<Type> PublicSignatureTypes(Type contract)
    {
        yield return contract;
        foreach (ConstructorInfo constructor in contract.GetConstructors(BindingFlags.Instance | BindingFlags.Public))
            foreach (ParameterInfo parameter in constructor.GetParameters()) yield return parameter.ParameterType;
        foreach (PropertyInfo property in contract.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            yield return property.PropertyType;
        foreach (MethodInfo method in contract.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            yield return method.ReturnType;
            foreach (ParameterInfo parameter in method.GetParameters()) yield return parameter.ParameterType;
        }
    }

    internal static ConstructorInfo ResolveServiceConstructor(
        Type serviceType,
        IReadOnlySet<string> expectedDependencies)
    {
        ProviderNeutralTypeGraph.EnsureProviderNeutralPublicConstructors(serviceType);
        return serviceType.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .SingleOrDefault(candidate => candidate.GetParameters()
                .Select(parameter => parameter.ParameterType.Name)
                .ToHashSet(StringComparer.Ordinal)
                .SetEquals(expectedDependencies))
            ?? throw Missing($"exact planned constructor for {serviceType.Name}");
    }

    internal static InvalidOperationException Missing(string surface) =>
        new($"Phase 20 product RED: missing {surface}.");

    private static async Task<object> AwaitResult(object? invocation, Type returnType)
    {
        if (invocation is Task task)
        {
            await task;
            return task.GetType().GetProperty("Result")?.GetValue(task)
                ?? throw Missing($"result from {returnType.Name}");
        }
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            Task converted = (Task)returnType.GetMethod("AsTask")!.Invoke(invocation, null)!;
            await converted;
            return converted.GetType().GetProperty("Result")!.GetValue(converted)!;
        }
        return invocation ?? throw Missing($"result from {returnType.Name}");
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value is null) return null;
        Type actual = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (actual.IsInstanceOfType(value)) return value;
        if (actual.IsEnum) return Enum.Parse(actual, value.ToString()!, ignoreCase: true);
        if (value is IEnumerable values && actual != typeof(string))
        {
            Type itemType = CollectionItem(actual);
            return TypedArray(itemType, values.Cast<object>().Select(item => ConvertValue(item, itemType)!));
        }
        if (actual == typeof(DateTimeOffset) && value is DateTime dateTime) return new DateTimeOffset(dateTime);
        return Convert.ChangeType(value, actual, System.Globalization.CultureInfo.InvariantCulture);
    }
}
