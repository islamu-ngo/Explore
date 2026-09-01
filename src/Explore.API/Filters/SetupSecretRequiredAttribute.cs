// ABOUTME: Action filter attribute that gates onboarding write endpoints behind the setup secret.
// ABOUTME: Uses TypeFilterAttribute pattern for DI-aware filtering with ISetupSecretProvider validation.

using Explore.API.ExceptionHandling;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Onboarding;
using Explore.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Explore.API.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SetupSecretRequiredAttribute : TypeFilterAttribute
{
    public SetupSecretRequiredAttribute(bool requireIncomplete = false) : base(typeof(SetupSecretRequiredFilter))
    {
        Arguments = [requireIncomplete];
    }

    private class SetupSecretRequiredFilter : IAsyncActionFilter
    {
        private readonly ISetupSecretProvider _setupSecretProvider;
        private readonly IInstanceBootstrapStateRepository _instanceBootstrapStateRepository;
        private readonly IInstanceBootstrapAuditLogger _bootstrapAuditLogger;
        private readonly bool _requireIncomplete;

        public SetupSecretRequiredFilter(
            ISetupSecretProvider setupSecretProvider,
            IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
            IInstanceBootstrapAuditLogger bootstrapAuditLogger,
            bool requireIncomplete)
        {
            _setupSecretProvider = setupSecretProvider;
            _instanceBootstrapStateRepository = instanceBootstrapStateRepository;
            _bootstrapAuditLogger = bootstrapAuditLogger;
            _requireIncomplete = requireIncomplete;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var isCompleted = _requireIncomplete
                && (await _instanceBootstrapStateRepository.GetCurrent(context.HttpContext.RequestAborted))?.Status
                    == InstanceBootstrapStatus.Completed;
            if (isCompleted || !_setupSecretProvider.IsSetupModeActive)
            {
                LogAudit(
                    context,
                    InstanceBootstrapAuditEventType.SetupModeInactive,
                    "inactive",
                    ApiProblemCodes.SetupAlreadyCompleted);

                context.Result = ApiProblemFactory.ToProblemResult(
                    ApiProblemFactory.CreateGoneProblem(
                        context.HttpContext,
                        "Setup already completed",
                        "Setup mode is no longer active for this instance.",
                        ApiProblemCodes.SetupAlreadyCompleted));
                return;
            }

            var secret = context.HttpContext.Request.Headers["X-Setup-Secret"].FirstOrDefault();
            if (!_setupSecretProvider.ValidateSecret(secret))
            {
                LogAudit(
                    context,
                    InstanceBootstrapAuditEventType.SetupSecretRejected,
                    "rejected",
                    "invalid_setup_secret");

                context.Result = ApiProblemFactory.ToProblemResult(
                    ApiProblemFactory.CreateForbiddenProblem(
                        context.HttpContext,
                        "Invalid setup secret",
                        "A valid setup secret is required for this bootstrap operation."));
                return;
            }

            LogAudit(context, InstanceBootstrapAuditEventType.SetupSecretAccepted, "accepted");
            await next();
        }

        private void LogAudit(
            ActionExecutingContext context,
            InstanceBootstrapAuditEventType eventType,
            string outcome,
            string? failureCode = null)
        {
            _bootstrapAuditLogger.Log(new InstanceBootstrapAuditEvent(
                eventType,
                Operation: "setup_secret_gate",
                Outcome: outcome,
                RouteName: ResolveRouteName(context),
                TraceId: context.HttpContext.TraceIdentifier,
                FailureCode: failureCode));
        }

        private static string? ResolveRouteName(ActionExecutingContext context)
            => context.ActionDescriptor.AttributeRouteInfo?.Name
               ?? context.ActionDescriptor.DisplayName
               ?? context.HttpContext.Request.Path.Value;
    }
}
