// ABOUTME: Resolves authorization attribute values into supported primitive identifier types.
// ABOUTME: Accepts existing typed and string representations without coercing mismatched values.

namespace Explore.Application.Helpers;

public static class AttributeResolver
{
    public static bool TryGetGuid(object? value, out Guid result)
    {
        result = Guid.Empty;

        if (value is Guid guid)
        {
            result = guid;
            return true;
        }

        return value is string text && Guid.TryParse(text, out result);
    }

    public static bool TryGetInt(object? value, out int result)
    {
        result = 0;

        if (value is int integer)
        {
            result = integer;
            return true;
        }

        if (value is long longInteger && longInteger is >= int.MinValue and <= int.MaxValue)
        {
            result = (int)longInteger;
            return true;
        }

        return value is string text && int.TryParse(text, out result);
    }
}
