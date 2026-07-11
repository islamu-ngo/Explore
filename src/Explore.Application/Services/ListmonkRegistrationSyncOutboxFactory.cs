// ABOUTME: Resolves Listmonk tenant settings and creates registration subscriber sync outbox rows.
// ABOUTME: Serializes the Listmonk subscriber payload for a later background worker without performing external I/O.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;

namespace Explore.Application.Services;

public sealed class ListmonkRegistrationSyncOutboxFactory(IHierarchicalSettingsResolver settingsResolver)
    : IListmonkRegistrationSyncOutboxFactory
{
    public async Task<IntegrationSyncOutbox?> CreateForRegistrationAsync(
        Event eventEntity,
        User user,
        CreateEventRegistrationDto dto,
        Guid registrationIntentId,
        CancellationToken cancellationToken)
    {
        if (!dto.ShareEmailWithOrganizer || user.EmailVerified != true || string.IsNullOrWhiteSpace(user.Email))
        {
            return null;
        }

        var context = new SettingContext(TenantId: eventEntity.TenantId);
        var enabled = await settingsResolver.ResolveAsync<bool>(
            GovernanceSettingKeys.Integrations.Listmonk.Enabled,
            context,
            cancellationToken);
        var syncOnRegistration = await settingsResolver.ResolveAsync<bool>(
            GovernanceSettingKeys.Integrations.Listmonk.SyncOnRegistration,
            context,
            cancellationToken);
        var listId = await settingsResolver.ResolveAsync<int>(
            GovernanceSettingKeys.Integrations.Listmonk.DefaultListId,
            context,
            cancellationToken);

        if (!enabled || !syncOnRegistration || listId <= 0)
        {
            return null;
        }

        var preconfirm = await settingsResolver.ResolveAsync<bool>(
            GovernanceSettingKeys.Integrations.Listmonk.PreconfirmSubscriptions,
            context,
            cancellationToken);
        var subscriberName = string.Join(' ', new[] { user.FirstName, user.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part)))
            .Trim();
        if (subscriberName.Length == 0)
        {
            subscriberName = user.Email;
        }

        var payload = JsonSerializer.Serialize(new
        {
            email = user.Email,
            name = subscriberName,
            status = "enabled",
            lists = new[] { listId },
            preconfirm_subscriptions = preconfirm,
            attribs = new
            {
                tenant_id = eventEntity.TenantId,
                event_id = eventEntity.Id,
                event_title = eventEntity.Title,
                registration_intent_id = registrationIntentId,
                user_id = user.Id,
                consent_ui_version = dto.ConsentUiVersion
            }
        });

        return new IntegrationSyncOutbox
        {
            TenantId = eventEntity.TenantId,
            Tenant = null!,
            Kind = IntegrationKind.Listmonk,
            SourceType = EventLifecycleEmailOutboxFactory.RegistrationIntentSourceType,
            SourceId = registrationIntentId,
            EventId = eventEntity.Id,
            UserId = user.Id,
            SubscriberEmail = user.Email,
            SubscriberName = subscriberName,
            SubscriberPayloadJson = payload,
            ListmonkListId = listId,
            PreconfirmSubscriptions = preconfirm,
            CorrelationId = registrationIntentId.ToString()
        };
    }
}
