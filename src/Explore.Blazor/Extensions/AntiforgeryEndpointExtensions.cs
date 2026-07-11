// ABOUTME: Minimal API antiforgery validation helpers for state-changing BFF endpoints.
// ABOUTME: Applies explicit request-token validation and short-circuits with ProblemDetails on failure.

using Explore.Blazor.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Blazor.Extensions;

public static class AntiforgeryEndpointExtensions
{
    public static TBuilder ValidateAntiforgery<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter(async (context, next) =>
        {
            var selfCallTokenService = context.HttpContext.RequestServices.GetService<IBffSelfCallTokenService>();
            if (selfCallTokenService?.Validate(context.HttpContext) == true)
            {
                return await next(context);
            }

            var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();

            try
            {
                await antiforgery.ValidateRequestAsync(context.HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.Problem(
                    detail: "The antiforgery token was missing or invalid.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Antiforgery validation failed");
            }

            return await next(context);
        });

        return builder;
    }
}
