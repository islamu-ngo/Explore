// ABOUTME: Defines currency-qualified paid-event sales and review ceilings for one policy version.
// ABOUTME: Prevents mixed-currency policies from sharing ambiguous minor-unit thresholds.

using Explore.Domain.ValueObjects;

namespace Explore.Domain;

public sealed class PaidEventPolicyCurrencyRiskLimit
{
    private PaidEventPolicyCurrencyRiskLimit(
        string currencyCode,
        long? perEventSalesCeilingMinor,
        long? rollingOrganizerSalesCeilingMinor,
        long? highValueReviewThresholdMinor)
    {
        CurrencyCode = currencyCode;
        PerEventSalesCeilingMinor = perEventSalesCeilingMinor;
        RollingOrganizerSalesCeilingMinor = rollingOrganizerSalesCeilingMinor;
        HighValueReviewThresholdMinor = highValueReviewThresholdMinor;
    }

    public string CurrencyCode { get; }

    public long? PerEventSalesCeilingMinor { get; }

    public long? RollingOrganizerSalesCeilingMinor { get; }

    public long? HighValueReviewThresholdMinor { get; }

    public static PaidEventPolicyCurrencyRiskLimit Create(
        string currencyCode,
        long? perEventSalesCeilingMinor,
        long? rollingOrganizerSalesCeilingMinor,
        long? highValueReviewThresholdMinor)
    {
        CurrencyMetadata currency = CurrencyMetadata.Get(currencyCode);
        if (currency.IsNoCurrency)
        {
            throw new ArgumentException("Currency risk limits require a monetary currency.", nameof(currencyCode));
        }

        ValidatePositive(perEventSalesCeilingMinor, nameof(perEventSalesCeilingMinor));
        ValidatePositive(rollingOrganizerSalesCeilingMinor, nameof(rollingOrganizerSalesCeilingMinor));
        ValidatePositive(highValueReviewThresholdMinor, nameof(highValueReviewThresholdMinor));

        return new PaidEventPolicyCurrencyRiskLimit(
            currency.Code,
            perEventSalesCeilingMinor,
            rollingOrganizerSalesCeilingMinor,
            highValueReviewThresholdMinor);
    }

    private static void ValidatePositive(long? value, string parameterName)
    {
        if (value is <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
