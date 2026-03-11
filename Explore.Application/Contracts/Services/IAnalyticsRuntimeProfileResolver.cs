// ABOUTME: Contract for the analytics runtime profile resolver — the core consent policy engine.
// ABOUTME: Used by query handlers, admin UI, and tests to compute effective analytics behavior.

namespace Explore.Application.Contracts.Services;

using Explore.Application.Analytics;
using Explore.Application.Settings.Groups;

public interface IAnalyticsRuntimeProfileResolver
{
    AnalyticsRuntimeProfile Resolve(AnalyticsSettingGroup settings);
}
