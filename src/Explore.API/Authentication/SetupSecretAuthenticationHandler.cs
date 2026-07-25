// ABOUTME: Authenticates setup-secret authority only for the two canonical instance provider PATCH routes.
// ABOUTME: Fails closed without placing setup secret material in principals, logs, responses, or exceptions.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Explore.API.ExceptionHandling;
using Explore.Application.Constants;
using Explore.Application.Contracts.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Explore.API.Authentication;

public sealed class SetupSecretAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISetupSecretProvider setupSecretProvider)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string HeaderName = "X-Setup-Secret";

    private const string AuthProviderPath = "/api/instance/settings/auth-provider";
    private const string AuthorizationProviderPath = "/api/instance/settings/authz-provider";
    private bool _setupModeInactive;

    internal static bool SupportsRequest(HttpRequest request)
        => HttpMethods.IsPatch(request.Method)
           && (string.Equals(request.Path.Value, AuthProviderPath, StringComparison.OrdinalIgnoreCase)
               || string.Equals(request.Path.Value, AuthorizationProviderPath, StringComparison.OrdinalIgnoreCase));

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!SupportsRequest(Request))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Request.Headers.TryGetValue(HeaderName, out var values)
            || values.Count != 1)
        {
            return Task.FromResult(AuthenticateResult.Fail("Setup-secret authentication failed."));
        }

        if (!setupSecretProvider.IsSetupModeActive)
        {
            _setupModeInactive = true;
            return Task.FromResult(AuthenticateResult.Fail("Setup-secret authentication failed."));
        }

        if (setupSecretProvider.IsTimedOut
            || !setupSecretProvider.ValidateSecret(values[0]))
        {
            return Task.FromResult(AuthenticateResult.Fail("Setup-secret authentication failed."));
        }

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(authenticationType: ApiAuthenticationSchemeNames.SetupSecret));
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(principal, Scheme.Name)));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (_setupModeInactive)
        {
            var problemDetails = ApiProblemFactory.CreateGoneProblem(
                Context,
                "Setup already completed",
                "Setup mode is no longer active for this instance.",
                ApiProblemCodes.SetupAlreadyCompleted);
            Response.StatusCode = StatusCodes.Status410Gone;
            var problemDetailsService = Context.RequestServices.GetRequiredService<IProblemDetailsService>();
            await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = Context,
                ProblemDetails = problemDetails
            });
            return;
        }

        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = $"{ApiAuthenticationSchemeNames.SetupSecret} realm=\"setup\"";
    }
}
