// ABOUTME: Action filter attribute that gates onboarding write endpoints behind the setup secret.
// ABOUTME: Uses TypeFilterAttribute pattern for DI-aware filtering with ISetupSecretProvider validation.

using Explore.Application.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Explore.API.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SetupSecretRequiredAttribute : TypeFilterAttribute
{
    public SetupSecretRequiredAttribute() : base(typeof(SetupSecretRequiredFilter))
    {
    }

    private class SetupSecretRequiredFilter : IAsyncActionFilter
    {
        private readonly ISetupSecretProvider _setupSecretProvider;

        public SetupSecretRequiredFilter(ISetupSecretProvider setupSecretProvider)
        {
            _setupSecretProvider = setupSecretProvider;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!_setupSecretProvider.IsSetupModeActive)
            {
                context.Result = new ObjectResult(new { error = "Setup already completed." })
                {
                    StatusCode = StatusCodes.Status410Gone,
                    ContentTypes = { "application/json" }
                };
                return;
            }

            var secret = context.HttpContext.Request.Headers["X-Setup-Secret"].FirstOrDefault();
            if (!_setupSecretProvider.ValidateSecret(secret))
            {
                context.Result = new ObjectResult(new { error = "Invalid setup secret." })
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                    ContentTypes = { "application/json" }
                };
                return;
            }

            await next();
        }
    }
}
