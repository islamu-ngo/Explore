// ABOUTME: Global Verify snapshot configuration for API integration contract tests.
// ABOUTME: Enables strict JSON rendering while per-test settings control snapshot directories.

using System.Runtime.CompilerServices;
using VerifyTests;

namespace Event.Api.IntegrationTests;

public static class VerifySnapshotSettings
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifierSettings.UseStrictJson();
    }
}
