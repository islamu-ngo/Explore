// ABOUTME: Defines bounded fixed-endpoint configuration for the global ATProto Jetstream subscriber.
// ABOUTME: Validates lease, retry, message-size, and optional bounded DID filter entries at startup.

using CarpaNet;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Services.Federation;

public sealed class AtprotoJetstreamOptions
{
    public const string SectionName = "Atproto:Jetstream";
    public string Endpoint { get; set; } = "https://jetstream1.us-east.bsky.network";
    public int MaxMessageSizeBytes { get; set; } = 2_113_536;
    public int LeaseDurationSeconds { get; set; } = 60;
    public int LeaseRenewalSeconds { get; set; } = 20;
    public int CapabilityPollMilliseconds { get; set; } = 5_000;
    public int RetryMinimumMilliseconds { get; set; } = 1_000;
    public int RetryMaximumMilliseconds { get; set; } = 30_000;
    public string[] AllowedDids { get; set; } = [];
}

public sealed class AtprotoJetstreamOptionsValidator : IValidateOptions<AtprotoJetstreamOptions>
{
    public ValidateOptionsResult Validate(string? name, AtprotoJetstreamOptions options)
    {
        var failures = new List<string>();
        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out Uri? endpoint)
            || !string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
            || !string.IsNullOrEmpty(endpoint.UserInfo)
            || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment)
            || endpoint.AbsolutePath is not ("" or "/"))
        {
            failures.Add("Atproto:Jetstream:Endpoint must be a fixed HTTPS origin without credentials, path, query, or fragment.");
        }

        if (options.MaxMessageSizeBytes < AtprotoRecordSizeValidator.MaximumJsonBytes
            || options.MaxMessageSizeBytes > 2_162_688)
        {
            failures.Add("Atproto:Jetstream:MaxMessageSizeBytes must be between 2097152 and 2162688 bytes.");
        }

        if (options.LeaseDurationSeconds is < 15 or > 300
            || options.LeaseRenewalSeconds is < 5 or > 120
            || options.LeaseRenewalSeconds >= options.LeaseDurationSeconds)
        {
            failures.Add("Jetstream lease renewal must be between 5 and 120 seconds and shorter than the 15-300 second lease.");
        }

        if (options.CapabilityPollMilliseconds is < 100 or > 60_000
            || options.RetryMinimumMilliseconds is < 10 or > 60_000
            || options.RetryMaximumMilliseconds < options.RetryMinimumMilliseconds
            || options.RetryMaximumMilliseconds > 300_000)
        {
            failures.Add("Jetstream polling and retry intervals are outside their bounded ranges.");
        }

        if (options.AllowedDids is not { Length: <= 10_000 }
            || options.AllowedDids.Any(did => !IsValidDid(did)))
        {
            failures.Add("Atproto:Jetstream:AllowedDids must contain at most 10000 valid DIDs when filtering is configured.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsValidDid(string did)
    {
        try
        {
            return did.Length <= 255 && ATDid.IsValid(did);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
