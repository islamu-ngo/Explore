// ABOUTME: Executes each incoming webhook claim in a fresh tenant-bound DI scope and machine principal.
// ABOUTME: Authorizes the narrow processing action and clears all ambient execution state on every exit path.

using Explore.Application.Authentication;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Webhooks;
using Explore.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Infrastructure.Webhooks;

public sealed class IncomingWebhookTenantExecutor(IServiceScopeFactory scopeFactory) : IIncomingWebhookClaimExecutor
{
    public async Task<IncomingWebhookClaimExecutionResult> ExecuteAsync(
        IncomingWebhookClaim claim,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var tenantAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        var machineAccessor = scope.ServiceProvider.GetRequiredService<IMachinePrincipalExecutionAccessor>();
        var principal = new ApiKeyPrincipalContext(
            $"internal:webhook-incoming:{claim.TenantId:N}",
            claim.TenantId,
            ExternalApiKeyOwnerType.Tenant,
            claim.TenantId,
            [InternalMachineScopes.ProcessIncomingWebhook]);

        tenantAccessor.SetTenant(claim.TenantId);
        machineAccessor.SetPrincipal(principal);

        try
        {
            var authorizationProvider = scope.ServiceProvider.GetRequiredService<IAuthorizationProvider>();
            var allowed = await authorizationProvider.IsAllowedAsync(
                ResourceKinds.Webhook,
                claim.IncomingWebhookMessageId.ToString("N"),
                AuthorizationActions.Webhooks.ProcessIncoming,
                new Dictionary<string, object>
                {
                    ["tenantId"] = claim.TenantId.ToString()
                },
                cancellationToken);
            if (!allowed)
            {
                return IncomingWebhookClaimExecutionResult.AuthorizationDenied();
            }

            var processor = scope.ServiceProvider.GetRequiredService<IIncomingWebhookProcessingService>();
            return await processor.ProcessAsync(claim, cancellationToken);
        }
        finally
        {
            machineAccessor.Clear();
            tenantAccessor.Clear();
        }
    }
}
