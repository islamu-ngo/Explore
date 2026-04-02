// ABOUTME: Parallel limiter for browser E2E tests to prevent resource exhaustion.
// ABOUTME: Caps concurrent Playwright tests at 2 to avoid WASM hydration contention.

namespace Explore.Blazor.Client.E2ETests;

public class BrowserParallelLimit : IParallelLimit
{
    public int Limit => 2;
}
