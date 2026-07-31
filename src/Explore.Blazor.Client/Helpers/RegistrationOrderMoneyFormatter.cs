// ABOUTME: Formats server-computed integer-minor registration-order amounts for display.
// ABOUTME: Uses decimal arithmetic only for presentation and never derives or submits checkout totals.

namespace Explore.Blazor.Client.Helpers;

public static class RegistrationOrderMoneyFormatter
{
    public static string Format(long? amountMinor, string? currencyCode) =>
        Format(amountMinor.GetValueOrDefault(), currencyCode);

    public static string Format(long amountMinor, string? currencyCode)
    {
        var currency = string.IsNullOrWhiteSpace(currencyCode) ? string.Empty : $" {currencyCode.Trim()}";
        return $"{amountMinor:N0}{currency} minor units";
    }
}
