// ABOUTME: Walks complete public type graphs for provider-specific admission contract leakage.
// ABOUTME: Traverses arrays, nullable and generic arguments, base types, and interfaces with cycle protection.

namespace ApplicationUnitTests.Contracts.Admissions.Support;

internal static class ProviderNeutralTypeGraph
{
    internal static IReadOnlyCollection<Type> Closure(IEnumerable<Type> roots)
    {
        var visited = new HashSet<Type>();
        var pending = new Stack<Type>(roots);
        while (pending.TryPop(out Type? type))
        {
            if (!visited.Add(type)) continue;

            if (type.HasElementType && type.GetElementType() is { } elementType)
                pending.Push(elementType);

            Type? nullable = Nullable.GetUnderlyingType(type);
            if (nullable is not null) pending.Push(nullable);

            if (type.IsGenericType)
            {
                pending.Push(type.GetGenericTypeDefinition());
                foreach (Type argument in type.GetGenericArguments()) pending.Push(argument);
            }

            if (type.BaseType is not null) pending.Push(type.BaseType);
            foreach (Type contract in type.GetInterfaces()) pending.Push(contract);
        }

        return visited;
    }

    internal static void EnsureProviderNeutralPublicConstructors(Type serviceType)
    {
        Type[] constructorTypes = serviceType.GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .ToArray();
        if (Closure(constructorTypes).Any(IsProviderSpecific))
            throw AdmissionContractRuntime.Missing($"provider-neutral public constructors for {serviceType.Name}");
    }

    internal static bool IsProviderSpecific(Type type)
    {
        string name = type.FullName ?? type.Name;
        return name.Contains("Stripe", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("PaymentIntent", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("RefundProvider", StringComparison.OrdinalIgnoreCase);
    }
}
