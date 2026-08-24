// ABOUTME: Applies tenant-admin evidence-based recovery to one parked IntegrationSync provider outcome.
// ABOUTME: Authorizes tenant scope and conditionally resolves only the exact ambiguous durable row.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Integrations.Validators;
using Explore.Application.Features.Integrations.Listmonk.Requests.Commands;
using Explore.Application.Features.Settings.Handlers;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Settings;
using MediatR;

namespace Explore.Application.Features.Integrations.Listmonk.Handlers.Commands;

public sealed class ResolveIntegrationSyncAmbiguityCommandHandler(
    IIntegrationSyncOutboxRepository repository,
    IAdminContext adminContext,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
    : IRequestHandler<ResolveIntegrationSyncAmbiguityCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        ResolveIntegrationSyncAmbiguityCommand request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var validator = new ResolveIntegrationSyncAmbiguityDtoValidator();
        var validation = await validator.ValidateAsync(request.Resolution, cancellationToken);
        if (!validation.IsValid || request.OutboxId == Guid.Empty)
        {
            response.Message = "Integration sync recovery validation failed.";
            response.Errors = validation.Errors.Select(error => error.ErrorMessage).ToList();
            if (request.OutboxId == Guid.Empty)
            {
                response.Errors.Add("OutboxId is required.");
            }
            return response;
        }

        var authorization = await SettingCommandHelper.CheckAuthorizationAsync(
            SettingScope.Tenant,
            adminContext,
            tenantContext,
            currentUserService,
            cancellationToken);
        Guid? actorId = await SettingCommandHelper.ResolveCurrentUserIdAsync(
            adminContext,
            currentUserService,
            cancellationToken);
        if (!authorization.Authorized || actorId is null)
        {
            response.Message = authorization.Error ?? "Tenant administrator authorization is required.";
            return response;
        }

        IntegrationSyncOutbox? resolved = await repository.ResolveAmbiguousAsync(
            new IntegrationSyncRecoveryRequest(
                tenantContext.TenantId,
                request.OutboxId,
                request.Resolution.Decision,
                request.Resolution.EvidenceReference.Trim(),
                actorId.Value,
                timeProvider.GetUtcNow().UtcDateTime),
            cancellationToken);
        if (resolved is null)
        {
            response.Message = "The ambiguous integration sync row was not found or was already resolved.";
            return response;
        }

        response.Success = true;
        response.Id = resolved.Id;
        response.Message = "Integration sync ambiguity resolved.";
        return response;
    }
}
