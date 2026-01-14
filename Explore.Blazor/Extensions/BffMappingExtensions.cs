using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Explore.Blazor.Extensions;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Extensions
{
    public static class BffMappingExtensions
    {
        /// <summary>
        /// Map a protected GET endpoint that requires authorization and uses the authenticated API client.
        /// </summary>
        public static IEndpointConventionBuilder MapProtectedGet<T>(this IEndpointRouteBuilder group, string pattern, Func<IEventApiClient, Task<T>> apiCall, ILogger logger)
        {
            var builder = group.MapGet(pattern, async (HttpContext ctx) =>
            {
                return await BffApiExtensions.ExecuteAsync(
                    () => apiCall(ctx.GetApiClient()),
                    logger,
                    $"GET {pattern}",
                    ctx
                );
            }).RequireAuthorization();

            return builder;
        }
    }
}
