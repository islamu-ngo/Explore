// ABOUTME: Handles explicit authorization policy package sync requests from setup and admin flows.
// ABOUTME: Maps provider-neutral package publish results to safe command responses for UI retry handling.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public sealed class SyncAuthorizationPolicyPackageCommandHandler(
    IPolicyPackageService policyPackageService,
    IAuthorizationProviderConfigurationService configurationService,
    ILogger<SyncAuthorizationPolicyPackageCommandHandler> logger,
    IAuthorizationRevisionProvider? revisionProvider = null)
    : IRequestHandler<SyncAuthorizationPolicyPackageCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        SyncAuthorizationPolicyPackageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var configuration = await configurationService.ReadConfigurationAsync();
            if (configuration.AuthorizationProviderManagedByDeployment)
            {
                var reconciliation = await configurationService
                    .ReconcileDeploymentProviderAsync(cancellationToken);
                return new BaseCommandResponse<Guid>
                {
                    Success = reconciliation.Succeeded,
                    Message = reconciliation.Message,
                    Errors = reconciliation.Succeeded ? [] : [reconciliation.Message]
                };
            }

            var result = await policyPackageService.PublishAsync(cancellationToken);

            // The store just changed, so any cached revision now describes the previous policy set.
            // Invalidate on failure too: a partial publish also moves the store, and leaving a stale
            // "certain" revision behind would let sensitive actions run against a half-published package.
            revisionProvider?.Invalidate();

            if (result.Succeeded)
            {
                logger.LogInformation(
                    "Authorization policy package synced. PackageId={PackageId} ContentHash={ContentHash}",
                    result.PackageId,
                    result.ContentHash);

                return new BaseCommandResponse<Guid>
                {
                    Id = Guid.Empty,
                    Success = true,
                    Message = string.IsNullOrWhiteSpace(result.Message)
                        ? "Authorization policy package synced successfully."
                        : result.Message
                };
            }

            logger.LogWarning(
                "Authorization policy package sync failed. PackageId={PackageId} IssueCode={IssueCode} Message={Message}",
                result.PackageId,
                result.IssueCode,
                result.Message);

            return new BaseCommandResponse<Guid>
            {
                Id = Guid.Empty,
                Success = false,
                Message = string.IsNullOrWhiteSpace(result.Message)
                    ? "Authorization policy package sync failed."
                    : result.Message,
                FailureCode = result.IssueCode == PolicyPackageIssueCode.None ? null : result.IssueCode.ToString(),
                Errors = result.Warnings.ToList()
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Authorization policy package sync failed unexpectedly.");
            return new BaseCommandResponse<Guid>
            {
                Id = Guid.Empty,
                Success = false,
                Message = "Authorization policy package sync failed.",
                Errors = ["Review server logs for the safe failure details and retry."]
            };
        }
    }
}
