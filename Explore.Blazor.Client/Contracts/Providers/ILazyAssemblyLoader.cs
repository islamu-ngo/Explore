// ABOUTME: Contract for lazy loading client assemblies.
// ABOUTME: Keeps runtime assembly loading behind a testable abstraction.

using System.Reflection;

namespace Explore.Blazor.Client.Contracts.Providers;

public interface ILazyAssemblyLoader
{
    Task<List<Assembly>> LoadAssembliesAsync(params string[] assemblyNames);
}
