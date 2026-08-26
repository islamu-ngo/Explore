// ABOUTME: Encodes normalized invariant Unicode text as delimiter-aligned fixed-width ASCII scalar tokens.
// ABOUTME: Freezes version-one address substring and display ordering keys behind independent contracts.

using System.Buffers;
using System.Globalization;
using System.Text;

namespace Explore.Domain.ValueObjects;

internal static class UnicodeScalarKeyV1
{
    internal const int TokenWidth = 7;

    internal static string Encode(string value, int maximumLength)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length == 0)
        {
            throw new ArgumentException("A non-empty Unicode value is required.", nameof(value));
        }

        string normalized = Normalize(value);
        var builder = new StringBuilder(Math.Min(normalized.Length * TokenWidth, maximumLength));
        ReadOnlySpan<char> remaining = normalized.AsSpan();
        while (!remaining.IsEmpty)
        {
            OperationStatus status = Rune.DecodeFromUtf16(remaining, out Rune rune, out int consumed);
            if (status != OperationStatus.Done)
            {
                throw new ArgumentException("The Unicode value is malformed.", nameof(value));
            }

            if (builder.Length > maximumLength - TokenWidth)
            {
                throw new ArgumentException("The normalized Unicode value exceeds the supported length.", nameof(value));
            }

            builder.Append('U');
            builder.Append(rune.Value.ToString("X6", CultureInfo.InvariantCulture));
            remaining = remaining[consumed..];
        }

        if (builder.Length == 0)
        {
            throw new ArgumentException("A non-empty Unicode value is required.", nameof(value));
        }

        return builder.ToString();
    }

    private static string Normalize(string value)
    {
        var normalized = new StringBuilder(value.Length);
        var segment = new StringBuilder(value.Length);
        ReadOnlySpan<char> remaining = value.AsSpan();
        while (!remaining.IsEmpty)
        {
            OperationStatus status = Rune.DecodeFromUtf16(remaining, out Rune rune, out int consumed);
            if (status != OperationStatus.Done)
            {
                throw new ArgumentException("The Unicode value is malformed.", nameof(value));
            }

            if (IsNonCharacter(rune.Value))
            {
                AppendNormalizedSegment(normalized, segment);
                normalized.Append(rune.ToString());
            }
            else
            {
                segment.Append(rune.ToString());
            }
            remaining = remaining[consumed..];
        }

        AppendNormalizedSegment(normalized, segment);
        return normalized.ToString();
    }

    private static void AppendNormalizedSegment(StringBuilder output, StringBuilder segment)
    {
        if (segment.Length == 0)
        {
            return;
        }

        output.Append(segment.ToString()
            .Normalize(NormalizationForm.FormC)
            .ToUpperInvariant()
            .Normalize(NormalizationForm.FormC));
        segment.Clear();
    }

    private static bool IsNonCharacter(int value) =>
        value is >= 0xFDD0 and <= 0xFDEF || (value & 0xFFFF) is 0xFFFE or 0xFFFF;
}

internal static class LocationAddressSubstringKeyV1
{
    internal const short Version = 1;
    internal const int MaximumLength = 14_000;

    internal static string Create(string address) => UnicodeScalarKeyV1.Encode(address, MaximumLength);
}

internal static class LocationDisplaySortKeyV1
{
    internal const short Version = 1;
    internal const int MaximumLength = 14_000;

    internal static string Create(string fullName) => UnicodeScalarKeyV1.Encode(fullName, MaximumLength);
}
