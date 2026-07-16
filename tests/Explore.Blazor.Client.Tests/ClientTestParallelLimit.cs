// ABOUTME: Caps bUnit test concurrency to prevent renderer and thread-pool starvation.
// ABOUTME: Keeps independent component tests parallel while remaining stable beside other test projects.

using TUnit.Core;
using TUnit.Core.Interfaces;

[assembly: ParallelLimiter<Explore.Blazor.Client.Tests.ClientTestParallelLimit>]

namespace Explore.Blazor.Client.Tests;

public sealed class ClientTestParallelLimit : IParallelLimit
{
    public int Limit => 8;
}
