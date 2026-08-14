// ABOUTME: Validated Stripe payment configuration for test/live mode isolation.
// ABOUTME: Keeps Stripe mode evidence checks and option validation inside Infrastructure.

using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Payments.Stripe;

public sealed class StripePaymentOptions
{
    public const string SectionName = "Payments:Stripe";
    public const string TestMode = "Test";
    public const string LiveMode = "Live";

    public string Mode { get; set; } = TestMode;

    public bool ExpectsLiveMode => string.Equals(Mode, LiveMode, StringComparison.Ordinal);

    public string ExpectedSecretKeyPrefix => ExpectsLiveMode ? "sk_live_" : "sk_test_";
}

public sealed class StripePaymentOptionsValidator : IValidateOptions<StripePaymentOptions>
{
    public ValidateOptionsResult Validate(string? name, StripePaymentOptions options) => options.Mode switch
    {
        StripePaymentOptions.TestMode or StripePaymentOptions.LiveMode => ValidateOptionsResult.Success,
        _ => ValidateOptionsResult.Fail("Payments:Stripe:Mode must be Test or Live.")
    };
}

internal static class StripeModeEvidence
{
    public static bool Matches(StripePaymentOptions options, bool livemode) => livemode == options.ExpectsLiveMode;

    public static bool TryReadLivemode(JsonElement? element, out bool livemode)
    {
        livemode = false;
        if (element is not { ValueKind: JsonValueKind.Object } json
            || !json.TryGetProperty("livemode", out JsonElement livemodeElement)
            || livemodeElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return false;
        }

        livemode = livemodeElement.GetBoolean();
        return true;
    }

    public static bool TryReadEventLivemode(string payload, out bool livemode) =>
        TryReadPayloadBoolean(payload, static root => root, out livemode);

    public static bool TryReadAccountObjectLivemode(string payload, out bool livemode) =>
        TryReadPayloadBoolean(payload, static root => root.GetProperty("data").GetProperty("object"), out livemode);

    private static bool TryReadPayloadBoolean(
        string payload,
        Func<JsonElement, JsonElement> selectObject,
        out bool livemode)
    {
        livemode = false;
        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);
            JsonElement target = selectObject(document.RootElement);
            if (target.ValueKind != JsonValueKind.Object
                || !target.TryGetProperty("livemode", out JsonElement livemodeElement)
                || livemodeElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                return false;
            }

            livemode = livemodeElement.GetBoolean();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }
}
