// ABOUTME: Field-operation primitive for partial update bodies that need explicit clear-null semantics.
// ABOUTME: Distinguishes omitted fields from explicit set or clear operations inside present update groups.

namespace Explore.Application.Models.Common;

public readonly record struct OptionalUpdate<T>(bool HasValue, T? Value)
{
    public static OptionalUpdate<T> Unspecified() => default;

    public static OptionalUpdate<T> Set(T? value) => new(true, value);
}
