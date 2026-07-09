// ABOUTME: Handles authenticated-user notification preference cell saves.
// ABOUTME: Validates required and locked cells before writing user-scoped overrides transactionally.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.Notifications.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Handlers.Commands;

public sealed class UpdateCurrentUserNotificationPreferenceMatrixCommandHandler(
    INotificationChannelPreferenceRepository preferenceRepository,
    INotificationPreferenceResolver resolver,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateCurrentUserNotificationPreferenceMatrixCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateCurrentUserNotificationPreferenceMatrixCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Failure("User not authenticated.");
        }

        if (request.Cells.Count == 0)
        {
            return Failure("At least one preference cell is required.");
        }

        var categories = (await preferenceRepository.ListCategoriesAsync(cancellationToken))
            .ToDictionary(category => Normalize(category.MasterCode), StringComparer.Ordinal);
        var channels = (await preferenceRepository.ListChannelsAsync(cancellationToken))
            .ToDictionary(channel => Normalize(channel.MasterCode), StringComparer.Ordinal);
        var errors = new List<string>();
        var validated = new List<(int CategoryId, int ChannelId, bool IsEnabled)>();

        foreach (var cell in request.Cells)
        {
            var categoryCode = Normalize(cell.CategoryCode);
            var channelCode = Normalize(cell.ChannelCode);

            if (!categories.TryGetValue(categoryCode, out var category))
            {
                errors.Add($"Unknown notification preference category '{cell.CategoryCode}'.");
                continue;
            }

            if (!channels.TryGetValue(channelCode, out var channel))
            {
                errors.Add($"Unknown notification preference channel '{cell.ChannelCode}'.");
                continue;
            }

            if (category.IsRequired && !cell.IsEnabled)
            {
                errors.Add($"Category '{category.MasterCode}' is required and cannot be disabled.");
                continue;
            }

            var decision = await resolver.ResolveAsync(new NotificationPreferenceResolveRequest(
                tenantContext.TenantId,
                userId.Value,
                OrganizationId: null,
                GroupId: null,
                category.MasterCode,
                channel.MasterCode), cancellationToken);

            if (decision.IsLocked && decision.EffectiveSourceScope != "User")
            {
                errors.Add($"Preference '{category.MasterCode}/{channel.MasterCode}' is locked by {decision.EffectiveSourceScope}.");
                continue;
            }

            validated.Add((category.Id, channel.Id, cell.IsEnabled));
        }

        if (errors.Count > 0)
        {
            return Failure("Notification preference update failed.", errors);
        }

        Guid lastId = Guid.Empty;
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            foreach (var cell in validated)
            {
                var preference = await preferenceRepository.UpsertUserPreferenceAsync(
                    tenantContext.TenantId,
                    userId.Value,
                    cell.CategoryId,
                    cell.ChannelId,
                    cell.IsEnabled,
                    token);
                lastId = preference.Id;
            }
        }, cancellationToken);

        return new BaseCommandResponse<Guid>
        {
            Id = lastId,
            Success = true,
            Message = "Notification preferences updated."
        };
    }

    private static BaseCommandResponse<Guid> Failure(string message, List<string>? errors = null)
    {
        return new BaseCommandResponse<Guid>
        {
            Success = false,
            Message = message,
            Errors = errors ?? [message]
        };
    }

    private static string Normalize(string code) => code.Trim().ToLowerInvariant();
}
