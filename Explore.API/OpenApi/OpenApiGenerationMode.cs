using System.Reflection;

namespace Explore.API.OpenApi;

internal static class OpenApiGenerationMode
{
    private const string BuildTimeGeneratorAssemblyName = "GetDocument.Insider";

    public static bool IsBuildTimeGeneration => string.Equals(
        Assembly.GetEntryAssembly()?.GetName().Name,
        BuildTimeGeneratorAssemblyName,
        StringComparison.Ordinal);
}
