// ABOUTME: Caps API integration test concurrency to prevent TestServer host starvation.
// ABOUTME: Keeps TUnit parallelism bounded while allowing independent contract tests to overlap.

using TUnit.Core;
using TUnit.Core.Interfaces;

[assembly: ParallelLimiter<Event.Api.IntegrationTests.ApiTestParallelLimit>]

namespace Event.Api.IntegrationTests;

public sealed class ApiTestParallelLimit : IParallelLimit
{
    public int Limit => 8;
}
