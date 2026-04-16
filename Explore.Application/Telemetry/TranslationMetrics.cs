// ABOUTME: Custom OpenTelemetry metrics for the translation/localization system.
// ABOUTME: Tracks fetch latency, fallback activations, language changes, and TMS test connections at boundaries only.

using System.Diagnostics.Metrics;

namespace Explore.Application.Telemetry;

/// <summary>
/// Translation metrics exposed via OpenTelemetry.
/// <para>
/// <b>Important design constraint (D8):</b> these metrics are recorded at fetch/fallback/admin
/// boundaries only. The <c>T(key)</c> hot path must NEVER be instrumented — it must stay
/// allocation-free and zero-overhead.
/// </para>
/// Meter name: "Explore.Translation"
/// </summary>
public sealed class TranslationMetrics
{
    public const string MeterName = "Explore.Translation";

    private readonly Counter<long> _fetchTotal;
    private readonly Histogram<double> _fetchDuration;
    private readonly Counter<long> _changeLanguageTotal;
    private readonly Counter<long> _connectionTestTotal;
    private readonly Counter<long> _fallbackActivatedTotal;

    public TranslationMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _fetchTotal = meter.CreateCounter<long>(
            "islamu.translation.fetch_total",
            unit: "{fetch}",
            description: "Total translation fetches by provider, language, and result");

        _fetchDuration = meter.CreateHistogram<double>(
            "islamu.translation.fetch_duration_seconds",
            unit: "s",
            description: "Translation fetch latency in seconds by provider and language");

        _changeLanguageTotal = meter.CreateCounter<long>(
            "islamu.translation.change_language_total",
            unit: "{change}",
            description: "Total language change operations");

        _connectionTestTotal = meter.CreateCounter<long>(
            "islamu.tms.connection_test_total",
            unit: "{test}",
            description: "Total TMS connection test attempts by provider and result");

        _fallbackActivatedTotal = meter.CreateCounter<long>(
            "islamu.tms.fallback_activated_total",
            unit: "{fallback}",
            description: "Total TMS fallback activations — alertable if > 0 in 5m window");
    }

    /// <summary>
    /// Records a translation fetch at the provider boundary.
    /// Called in TolgeeTranslationProvider, WeblateTranslationProvider, and OfflineTranslationProvider.
    /// </summary>
    /// <param name="provider">Provider name: "tolgee", "weblate", "offline".</param>
    /// <param name="language">Language code being fetched.</param>
    /// <param name="result">Outcome: "hit_cache", "hit_tms", "hit_offline", "error".</param>
    public void RecordFetch(string provider, string language, string result)
    {
        _fetchTotal.Add(1,
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("language", language),
            new KeyValuePair<string, object?>("result", result));
    }

    /// <summary>
    /// Records the duration of a translation fetch operation.
    /// </summary>
    public void RecordFetchDuration(string provider, string language, double durationSeconds)
    {
        _fetchDuration.Record(durationSeconds,
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("language", language));
    }

    /// <summary>
    /// Records a language change event in TranslationService.ChangeLanguageAsync.
    /// </summary>
    public void RecordLanguageChange(string from, string to)
    {
        _changeLanguageTotal.Add(1,
            new KeyValuePair<string, object?>("from", from),
            new KeyValuePair<string, object?>("to", to));
    }

    /// <summary>
    /// Records a TMS connection test in TestTmsConnectionCommandHandler.
    /// </summary>
    public void RecordConnectionTest(string provider, string result)
    {
        _connectionTestTotal.Add(1,
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("result", result));
    }

    /// <summary>
    /// Records a TMS fallback activation in RuntimeTranslationProvider catch blocks.
    /// This metric is alertable — any increment in a 5-minute window should page on-call.
    /// </summary>
    /// <param name="provider">The provider that failed.</param>
    /// <param name="reason">Categorized reason: "timeout", "auth_error", "not_found", "rate_limited", "network_error", "other".</param>
    public void RecordFallbackActivated(string provider, string reason)
    {
        _fallbackActivatedTotal.Add(1,
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("reason", reason));
    }
}
