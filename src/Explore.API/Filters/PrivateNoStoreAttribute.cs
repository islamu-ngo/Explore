// ABOUTME: Marks sensitive read responses as browser-private and forbidden from storage.
// ABOUTME: Prevents physical-location payloads from entering shared or persistent HTTP caches.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace Explore.API.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class PrivateNoStoreAttribute : ActionFilterAttribute
{
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        context.HttpContext.Response.Headers[HeaderNames.CacheControl] = "private, no-store";
    }
}
