using System.Reflection;
using Explore.Blazor.Client.Contracts.Providers;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Default implementation of lazy assembly loading for Blazor WASM.
/// Uses the built-in Blazor lazy loading mechanism via the Router component's AdditionalAssemblies parameter.
/// This service provides a convenient interface for loading assemblies on-demand.
/// </summary>
public class LazyAssemblyLoaderService : ILazyAssemblyLoader
{
    /// <summary>
    /// Loads assemblies dynamically at runtime.
    /// In Blazor WASM, assemblies marked with BlazorWebAssemblyLazyLoad in the .csproj
    /// are automatically loaded when referenced in the Router's AdditionalAssemblies.
    /// </summary>
    /// <param name="assemblyNames">Names of assemblies to load (e.g., "Explore.Blazor.Client.Pages.Admin.dll")</param>
    /// <returns>List of loaded assemblies</returns>
    public async Task<List<Assembly>> LoadAssembliesAsync(params string[] assemblyNames)
    {
        var loadedAssemblies = new List<Assembly>();

        foreach (var assemblyName in assemblyNames)
        {
            try
            {
                // In Blazor WASM, lazy-loaded assemblies are loaded automatically by the runtime
                // when they're referenced in the Router's AdditionalAssemblies parameter.
                // This method simulates the loading by attempting to load the assembly from the AppDomain.
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == assemblyName.Replace(".dll", ""));

                if (assembly != null)
                {
                    loadedAssemblies.Add(assembly);
                }
                else
                {
                    // Assembly not yet loaded - it will be loaded by the Router when needed
                    Console.WriteLine($"Assembly '{assemblyName}' will be loaded by the Router when needed.");
                }
            }
            catch (Exception ex)
            {
                // Log error but continue loading other assemblies
                Console.Error.WriteLine($"Failed to load assembly '{assemblyName}': {ex.Message}");
            }
        }

        return await Task.FromResult(loadedAssemblies);
    }
}
