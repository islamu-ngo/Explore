// ABOUTME: Seeds all lookup/enum tables at runtime in ALL environments.
// ABOUTME: Replaces HasData() in entity configurations to avoid EF Core circular FK migration bug (#36682).

using Explore.Domain;
using Explore.Domain.Ai;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Modules;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Seed;

/// <summary>
/// Seeds lookup/enum tables at runtime. Runs in ALL environments (dev, staging, production).
///
/// Why runtime seeding instead of HasData():
/// EF Core 10 has a known bug (#36682) where dotnet ef migrations add crashes with
/// "Sequence contains no elements" when the model has circular FKs (User/Organization ↔ Actor)
/// combined with HasData on any entities. Moving seed data to runtime eliminates the issue.
///
/// The data was originally seeded via HasData() in entity configurations and exists in
/// existing migration files. This seeder ensures idempotent seeding for fresh databases
/// where migrations run in order.
/// </summary>
public static class LookupTableSeeder
{
    /// <summary>
    /// Seeds all lookup tables if they don't already contain data.
    /// Must be called after migrations are applied.
    /// </summary>
    public static async Task SeedAsync(ExploreDbContext context, CancellationToken cancellationToken = default)
    {
        await SeedActorTypesAsync(context, cancellationToken);
        await SeedActorSubscriptionStatusesAsync(context, cancellationToken);
        await SeedActorSubscriptionNotificationLevelsAsync(context, cancellationToken);
        await SeedRoleScopesAsync(context, cancellationToken);
        await SeedSettingScopesAsync(context, cancellationToken);
        await SeedSettingValueTypesAsync(context, cancellationToken);
        await SeedSecretSourceTypesAsync(context, cancellationToken);
        await SeedSecretValidationStatusesAsync(context, cancellationToken);
        await SeedExternalApiKeyOwnerTypesAsync(context, cancellationToken);
        await SeedNotificationScopeTypesAsync(context, cancellationToken);
        await SeedApprovalStatusesAsync(context, cancellationToken);
        await SeedAnalyticsProvidersAsync(context, cancellationToken);
        await SeedTenantStatusesAsync(context, cancellationToken);
        await SeedAudienceAgesAsync(context, cancellationToken);
        await SeedAudienceGendersAsync(context, cancellationToken);
        await SeedDidCustodyTypesAsync(context, cancellationToken);
        await SeedEventFormatsAsync(context, cancellationToken);
        await SeedEventStatusesAsync(context, cancellationToken);
        await SeedEventSessionStatusesAsync(context, cancellationToken);
        await SeedEventTypesAsync(context, cancellationToken);
        await SeedFileTypesAsync(context, cancellationToken);
        await SeedLanguagesAsync(context, cancellationToken);
        await SeedMadhabsAsync(context, cancellationToken);
        await SeedModuleDefinitionsAsync(context, cancellationToken);
        await SeedOrganizationPositionsAsync(context, cancellationToken);
        await SeedGroupPositionsAsync(context, cancellationToken);
        await SeedRegistrationModesAsync(context, cancellationToken);
        await SeedRolesAsync(context, cancellationToken);
        await SeedSystemSettingsAsync(context, cancellationToken);
        await SeedTagTypesAsync(context, cancellationToken);
        await SeedVisibilityTypesAsync(context, cancellationToken);
        await SeedPermissionsAsync(context, cancellationToken);
        await SeedEventRolePermissionsAsync(context, cancellationToken);
        await SeedNotificationTypesAsync(context, cancellationToken);
        await SeedNotificationEntityTypesAsync(context, cancellationToken);
        await SeedDefaultFooterLinkGroupsAsync(context, cancellationToken);
        await SeedExternalApiKeyStatusesAsync(context, cancellationToken);
        await SeedExternalApiKeyCreditPeriodsAsync(context, cancellationToken);
        await SeedNotificationReasonsAsync(context, cancellationToken);
        await SeedAiConversationStatusesAsync(context, cancellationToken);
        await SeedAiMessageRolesAsync(context, cancellationToken);
        await SeedAiRunStatusesAsync(context, cancellationToken);
        await SeedAiReferenceKindsAsync(context, cancellationToken);
        await SeedAiProposedActionKindsAsync(context, cancellationToken);
        await SeedAiProposedActionStatusesAsync(context, cancellationToken);
        await SeedAiProviderKindsAsync(context, cancellationToken);
        await SeedEventSessionKindsAsync(context, cancellationToken);
        await SeedScheduleItemKindsAsync(context, cancellationToken);
        await SeedEventRegistrationPoliciesAsync(context, cancellationToken);
        await SeedRegistrationScopesAsync(context, cancellationToken);
        await SeedUiThemePresetsAsync(context, cancellationToken);
    }

    private static async Task SeedAiConversationStatusesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<AiConversationStatusLookup>().AnyAsync(ct)) return;

        context.Set<AiConversationStatusLookup>().AddRange(
            new AiConversationStatusLookup { Id = (int)AiConversationStatus.Active, MasterCode = "ACTIVE", FullName = "Active", Description = "Conversation is available for user interaction" },
            new AiConversationStatusLookup { Id = (int)AiConversationStatus.Running, MasterCode = "RUNNING", FullName = "Running", Description = "Conversation has an in-flight AI provider run" },
            new AiConversationStatusLookup { Id = (int)AiConversationStatus.Blocked, MasterCode = "BLOCKED", FullName = "Blocked", Description = "Conversation cannot accept more messages" },
            new AiConversationStatusLookup { Id = (int)AiConversationStatus.Archived, MasterCode = "ARCHIVED", FullName = "Archived", Description = "Conversation is retained but no longer active" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedAiMessageRolesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<AiMessageRoleLookup>().AnyAsync(ct)) return;

        context.Set<AiMessageRoleLookup>().AddRange(
            new AiMessageRoleLookup { Id = (int)AiMessageRole.System, MasterCode = "SYSTEM", FullName = "System", Description = "System prompt or platform-authored instruction" },
            new AiMessageRoleLookup { Id = (int)AiMessageRole.User, MasterCode = "USER", FullName = "User", Description = "User-authored assistant message" },
            new AiMessageRoleLookup { Id = (int)AiMessageRole.Assistant, MasterCode = "ASSISTANT", FullName = "Assistant", Description = "AI assistant provider response" },
            new AiMessageRoleLookup { Id = (int)AiMessageRole.Tool, MasterCode = "TOOL", FullName = "Tool", Description = "Tool execution result supplied to the assistant" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedAiRunStatusesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<AiRunStatusLookup>().AnyAsync(ct)) return;

        context.Set<AiRunStatusLookup>().AddRange(
            new AiRunStatusLookup { Id = (int)AiRunStatus.Queued, MasterCode = "QUEUED", FullName = "Queued", Description = "Provider run has been queued" },
            new AiRunStatusLookup { Id = (int)AiRunStatus.InProgress, MasterCode = "IN_PROGRESS", FullName = "In progress", Description = "Provider run is executing" },
            new AiRunStatusLookup { Id = (int)AiRunStatus.Succeeded, MasterCode = "SUCCEEDED", FullName = "Succeeded", Description = "Provider run completed successfully" },
            new AiRunStatusLookup { Id = (int)AiRunStatus.Failed, MasterCode = "FAILED", FullName = "Failed", Description = "Provider run failed" },
            new AiRunStatusLookup { Id = (int)AiRunStatus.Cancelled, MasterCode = "CANCELLED", FullName = "Cancelled", Description = "Provider run was cancelled" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedAiReferenceKindsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<AiReferenceKindLookup>().AnyAsync(ct)) return;

        context.Set<AiReferenceKindLookup>().AddRange(
            new AiReferenceKindLookup { Id = (int)AiReferenceKind.Event, MasterCode = "EVENT", FullName = "Event", Description = "Conversation references an event" },
            new AiReferenceKindLookup { Id = (int)AiReferenceKind.EventSession, MasterCode = "EVENT_SESSION", FullName = "Event session", Description = "Conversation references an event session" },
            new AiReferenceKindLookup { Id = (int)AiReferenceKind.Actor, MasterCode = "ACTOR", FullName = "Actor", Description = "Conversation references an actor" },
            new AiReferenceKindLookup { Id = (int)AiReferenceKind.Organization, MasterCode = "ORGANIZATION", FullName = "Organization", Description = "Conversation references an organization" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedAiProposedActionKindsAsync(ExploreDbContext context, CancellationToken ct)
    {
        var requiredLookups = new[]
        {
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.CreateEventDraft, MasterCode = "CREATE_EVENT_DRAFT", FullName = "Create event draft", Description = "Create a draft event after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.UpdateEventDraft, MasterCode = "UPDATE_EVENT_DRAFT", FullName = "Update event draft", Description = "Propose draft event changes after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.PublishEvent, MasterCode = "PUBLISH_EVENT", FullName = "Publish event", Description = "Propose publishing an event after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.DeleteEvent, MasterCode = "DELETE_EVENT", FullName = "Delete event", Description = "Propose deleting an event after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.UpsertEventIslamicAspect, MasterCode = "UPSERT_EVENT_ISLAMIC_ASPECT", FullName = "Upsert event Islamic aspect", Description = "Propose saving an event Islamic aspect after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.DeleteEventIslamicAspect, MasterCode = "DELETE_EVENT_ISLAMIC_ASPECT", FullName = "Delete event Islamic aspect", Description = "Propose deleting an event Islamic aspect after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.UpsertEventTechAspect, MasterCode = "UPSERT_EVENT_TECH_ASPECT", FullName = "Upsert event Tech aspect", Description = "Propose saving an event Tech aspect after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.DeleteEventTechAspect, MasterCode = "DELETE_EVENT_TECH_ASPECT", FullName = "Delete event Tech aspect", Description = "Propose deleting an event Tech aspect after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.CreateEventSession, MasterCode = "CREATE_EVENT_SESSION", FullName = "Create event session", Description = "Propose creating an event session after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.UpdateEventSession, MasterCode = "UPDATE_EVENT_SESSION", FullName = "Update event session", Description = "Propose updating an event session after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.DeleteEventSession, MasterCode = "DELETE_EVENT_SESSION", FullName = "Delete event session", Description = "Propose deleting an event session after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.CreateEventSessionGroup, MasterCode = "CREATE_EVENT_SESSION_GROUP", FullName = "Create event session group", Description = "Propose creating an event session group after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.UpdateEventSessionGroup, MasterCode = "UPDATE_EVENT_SESSION_GROUP", FullName = "Update event session group", Description = "Propose updating an event session group after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.DeleteEventSessionGroup, MasterCode = "DELETE_EVENT_SESSION_GROUP", FullName = "Delete event session group", Description = "Propose deleting an event session group after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.AssignSessionToEventSessionGroup, MasterCode = "ASSIGN_SESSION_TO_EVENT_SESSION_GROUP", FullName = "Assign session to event session group", Description = "Propose assigning a session to an event session group after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.UnassignSessionFromEventSessionGroup, MasterCode = "UNASSIGN_SESSION_FROM_EVENT_SESSION_GROUP", FullName = "Unassign session from event session group", Description = "Propose unassigning a session from an event session group after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.CreateEventDay, MasterCode = "CREATE_EVENT_DAY", FullName = "Create event day", Description = "Propose creating an event day after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.UpdateEventDay, MasterCode = "UPDATE_EVENT_DAY", FullName = "Update event day", Description = "Propose updating an event day after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.DeleteEventDay, MasterCode = "DELETE_EVENT_DAY", FullName = "Delete event day", Description = "Propose deleting an event day after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.CreateEventAgendaItem, MasterCode = "CREATE_EVENT_AGENDA_ITEM", FullName = "Create event agenda item", Description = "Propose creating an event agenda item after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.UpdateEventAgendaItem, MasterCode = "UPDATE_EVENT_AGENDA_ITEM", FullName = "Update event agenda item", Description = "Propose updating an event agenda item after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.DeleteEventAgendaItem, MasterCode = "DELETE_EVENT_AGENDA_ITEM", FullName = "Delete event agenda item", Description = "Propose deleting an event agenda item after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.CreateEventCustomPropertyDefinition, MasterCode = "CREATE_EVENT_CUSTOM_PROPERTY_DEFINITION", FullName = "Create event custom property definition", Description = "Propose creating an event custom property definition after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.UpdateEventCustomPropertyDefinition, MasterCode = "UPDATE_EVENT_CUSTOM_PROPERTY_DEFINITION", FullName = "Update event custom property definition", Description = "Propose updating an event custom property definition after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.DeleteEventCustomPropertyDefinition, MasterCode = "DELETE_EVENT_CUSTOM_PROPERTY_DEFINITION", FullName = "Delete event custom property definition", Description = "Propose deleting an event custom property definition after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.PurgeEventCustomPropertyDefinition, MasterCode = "PURGE_EVENT_CUSTOM_PROPERTY_DEFINITION", FullName = "Purge event custom property definition", Description = "Propose purging an event custom property definition after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.SetEventCustomPropertyValue, MasterCode = "SET_EVENT_CUSTOM_PROPERTY_VALUE", FullName = "Set event custom property value", Description = "Propose setting an event custom property value after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.SetEventCustomPropertyMultiValues, MasterCode = "SET_EVENT_CUSTOM_PROPERTY_MULTI_VALUES", FullName = "Set event custom property multi-values", Description = "Propose replacing event custom property multi-values after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.CreateEventRegistration, MasterCode = "CREATE_EVENT_REGISTRATION", FullName = "Create event registration", Description = "Propose creating an event registration after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.UpdateEventRegistration, MasterCode = "UPDATE_EVENT_REGISTRATION", FullName = "Update event registration", Description = "Propose updating an event registration after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.DeleteEventRegistration, MasterCode = "DELETE_EVENT_REGISTRATION", FullName = "Delete event registration", Description = "Propose deleting an event registration after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.AssignEventTeamRole, MasterCode = "ASSIGN_EVENT_TEAM_ROLE", FullName = "Assign event team role", Description = "Propose assigning an event team role after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.RevokeEventTeamRole, MasterCode = "REVOKE_EVENT_TEAM_ROLE", FullName = "Revoke event team role", Description = "Propose revoking an event team role after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.CreateEventTemplate, MasterCode = "CREATE_EVENT_TEMPLATE", FullName = "Create event template", Description = "Propose creating an event template after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.UpdateEventTemplate, MasterCode = "UPDATE_EVENT_TEMPLATE", FullName = "Update event template", Description = "Propose updating an event template after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.DeleteEventTemplate, MasterCode = "DELETE_EVENT_TEMPLATE", FullName = "Delete event template", Description = "Propose deleting an event template after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.CreateEventSessionTemplate, MasterCode = "CREATE_EVENT_SESSION_TEMPLATE", FullName = "Create event session template", Description = "Propose creating an event session template after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.UpdateEventSessionTemplate, MasterCode = "UPDATE_EVENT_SESSION_TEMPLATE", FullName = "Update event session template", Description = "Propose updating an event session template after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.DeleteEventSessionTemplate, MasterCode = "DELETE_EVENT_SESSION_TEMPLATE", FullName = "Delete event session template", Description = "Propose deleting an event session template after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.ApplyEventTemplateSync, MasterCode = "APPLY_EVENT_TEMPLATE_SYNC", FullName = "Apply event template sync", Description = "Propose applying event template sync changes after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.ApplyEventSessionTemplateSync, MasterCode = "APPLY_EVENT_SESSION_TEMPLATE_SYNC", FullName = "Apply event session template sync", Description = "Propose applying event session template sync changes after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.LightModerateEvent, MasterCode = "LIGHT_MODERATE_EVENT", FullName = "Light moderate event", Description = "Propose reversible event moderation after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.HeavyModerateEvent, MasterCode = "HEAVY_MODERATE_EVENT", FullName = "Heavy moderate event", Description = "Propose irreversible event heavy moderation after human confirmation" },
            new AiProposedActionKindLookup { Id = (int)AiProposedActionKind.UnmoderateEvent, MasterCode = "UNMODERATE_EVENT", FullName = "Unmoderate event", Description = "Propose unmoderating a reversible event moderation after human confirmation" }
        };

        var existingIds = await context.Set<AiProposedActionKindLookup>()
            .Select(lookup => lookup.Id)
            .ToListAsync(ct);
        var missingLookups = requiredLookups
            .Where(lookup => !existingIds.Contains(lookup.Id))
            .ToArray();
        if (missingLookups.Length == 0) return;

        context.Set<AiProposedActionKindLookup>().AddRange(missingLookups);
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedAiProposedActionStatusesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<AiProposedActionStatusLookup>().AnyAsync(ct)) return;

        context.Set<AiProposedActionStatusLookup>().AddRange(
            new AiProposedActionStatusLookup { Id = (int)AiProposedActionStatus.Proposed, MasterCode = "PROPOSED", FullName = "Proposed", Description = "Action is awaiting human review" },
            new AiProposedActionStatusLookup { Id = (int)AiProposedActionStatus.Confirmed, MasterCode = "CONFIRMED", FullName = "Confirmed", Description = "Action was confirmed by a user" },
            new AiProposedActionStatusLookup { Id = (int)AiProposedActionStatus.Rejected, MasterCode = "REJECTED", FullName = "Rejected", Description = "Action was rejected by a user" },
            new AiProposedActionStatusLookup { Id = (int)AiProposedActionStatus.Executed, MasterCode = "EXECUTED", FullName = "Executed", Description = "Action side effect completed" },
            new AiProposedActionStatusLookup { Id = (int)AiProposedActionStatus.Failed, MasterCode = "FAILED", FullName = "Failed", Description = "Action side effect failed" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedAiProviderKindsAsync(ExploreDbContext context, CancellationToken ct)
    {
        var requiredLookups = new[]
        {
            new AiProviderKindLookup { Id = (int)AiProviderKind.None, MasterCode = "NONE", FullName = "None", Description = "AI provider is disabled" },
            new AiProviderKindLookup { Id = (int)AiProviderKind.Fake, MasterCode = "FAKE", FullName = "Fake", Description = "Deterministic fake provider for testing" },
            new AiProviderKindLookup { Id = (int)AiProviderKind.OpenAiCompatible, MasterCode = "OPENAI_COMPATIBLE", FullName = "OpenAI Compatible", Description = "Any OpenAI-compatible API endpoint" },
            new AiProviderKindLookup { Id = (int)AiProviderKind.AnthropicCompatible, MasterCode = "ANTHROPIC_COMPATIBLE", FullName = "Anthropic Compatible", Description = "Anthropic Messages API endpoint" },
            new AiProviderKindLookup { Id = (int)AiProviderKind.OpenAi, MasterCode = "OPENAI", FullName = "OpenAI", Description = "OpenAI Responses API endpoint" },
            new AiProviderKindLookup { Id = (int)AiProviderKind.AzureOpenAi, MasterCode = "AZURE_OPENAI", FullName = "Azure OpenAI", Description = "Microsoft.Extensions.AI with Azure OpenAI" },
            new AiProviderKindLookup { Id = (int)AiProviderKind.Anthropic, MasterCode = "ANTHROPIC", FullName = "Anthropic", Description = "Anthropic Messages API endpoint at api.anthropic.com" }
        };

        var existingLookups = await context.Set<AiProviderKindLookup>().ToListAsync(ct);
        var existingById = existingLookups.ToDictionary(lookup => lookup.Id);
        var changed = false;

        foreach (var requiredLookup in requiredLookups)
        {
            if (!existingById.TryGetValue(requiredLookup.Id, out var existingLookup))
            {
                context.Set<AiProviderKindLookup>().Add(requiredLookup);
                changed = true;
                continue;
            }

            if (existingLookup.MasterCode == requiredLookup.MasterCode
                && existingLookup.FullName == requiredLookup.FullName
                && existingLookup.Description == requiredLookup.Description)
            {
                continue;
            }

            existingLookup.MasterCode = requiredLookup.MasterCode;
            existingLookup.FullName = requiredLookup.FullName;
            existingLookup.Description = requiredLookup.Description;
            changed = true;
        }

        if (changed)
        {
            await context.SaveChangesAsync(ct);
        }
    }

    private static async Task SeedRegistrationScopesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<RegistrationScope>().AnyAsync(ct)) return;

        context.Set<RegistrationScope>().AddRange(
            new RegistrationScope { Id = (int)RegistrationScopeEnum.Event, MasterCode = "EVENT", FullName = "Whole event", Description = "User registered for the entire event" },
            new RegistrationScope { Id = (int)RegistrationScopeEnum.Day, MasterCode = "DAY", FullName = "Event day", Description = "User registered for a single event day" },
            new RegistrationScope { Id = (int)RegistrationScopeEnum.SessionSelection, MasterCode = "SESSION_SELECTION", FullName = "Session selection", Description = "User registered for a chosen set of sessions" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedEventRegistrationPoliciesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<EventRegistrationPolicy>().AnyAsync(ct)) return;

        context.Set<EventRegistrationPolicy>().AddRange(
            new EventRegistrationPolicy { Id = (int)EventRegistrationPolicyEnum.WholeEventOnly, MasterCode = "WHOLE_EVENT_ONLY", FullName = "Whole event only", Description = "Only whole-event registration is accepted" },
            new EventRegistrationPolicy { Id = (int)EventRegistrationPolicyEnum.WholeDayOnly, MasterCode = "WHOLE_DAY_ONLY", FullName = "Whole day only", Description = "Only whole-day registration is accepted" },
            new EventRegistrationPolicy { Id = (int)EventRegistrationPolicyEnum.SessionSelectionOnly, MasterCode = "SESSION_SELECTION_ONLY", FullName = "Session selection only", Description = "Only per-session selection is accepted" },
            new EventRegistrationPolicy { Id = (int)EventRegistrationPolicyEnum.WholeEventOrDay, MasterCode = "WHOLE_EVENT_OR_DAY", FullName = "Whole event or day", Description = "Whole-event and whole-day registrations are accepted" },
            new EventRegistrationPolicy { Id = (int)EventRegistrationPolicyEnum.WholeEventOrSession, MasterCode = "WHOLE_EVENT_OR_SESSION", FullName = "Whole event or session", Description = "Whole-event and per-session registrations are accepted" },
            new EventRegistrationPolicy { Id = (int)EventRegistrationPolicyEnum.Flexible, MasterCode = "FLEXIBLE", FullName = "Flexible", Description = "All registration scopes are accepted" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedScheduleItemKindsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<ScheduleItemKind>().AnyAsync(ct)) return;

        context.Set<ScheduleItemKind>().AddRange(
            new ScheduleItemKind { Id = (int)ScheduleItemKindEnum.Intro, MasterCode = "INTRO", FullName = "Intro", Description = "Opening remarks or welcome block" },
            new ScheduleItemKind { Id = (int)ScheduleItemKindEnum.Talk, MasterCode = "TALK", FullName = "Talk", Description = "Main speaker content block" },
            new ScheduleItemKind { Id = (int)ScheduleItemKindEnum.QAndA, MasterCode = "Q_AND_A", FullName = "Q&A", Description = "Audience questions and answers block" },
            new ScheduleItemKind { Id = (int)ScheduleItemKindEnum.Break, MasterCode = "BREAK", FullName = "Break", Description = "Refreshment or rest block" },
            new ScheduleItemKind { Id = (int)ScheduleItemKindEnum.Prayer, MasterCode = "PRAYER", FullName = "Prayer", Description = "Scheduled prayer block" },
            new ScheduleItemKind { Id = (int)ScheduleItemKindEnum.Outro, MasterCode = "OUTRO", FullName = "Outro", Description = "Closing remarks or farewell block" },
            new ScheduleItemKind { Id = (int)ScheduleItemKindEnum.Logistics, MasterCode = "LOGISTICS", FullName = "Logistics", Description = "Registration, seating, or housekeeping block" },
            new ScheduleItemKind { Id = (int)ScheduleItemKindEnum.Custom, MasterCode = "CUSTOM", FullName = "Custom", Description = "Tenant-defined block not covered by standard kinds" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedEventSessionKindsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<EventSessionKind>().AnyAsync(ct)) return;

        context.Set<EventSessionKind>().AddRange(
            new EventSessionKind { Id = (int)EventSessionKindEnum.Talk, MasterCode = "TALK", FullName = "Talk", Description = "A standard presentation or talk" },
            new EventSessionKind { Id = (int)EventSessionKindEnum.Workshop, MasterCode = "WORKSHOP", FullName = "Workshop", Description = "An interactive hands-on session" },
            new EventSessionKind { Id = (int)EventSessionKindEnum.Panel, MasterCode = "PANEL", FullName = "Panel", Description = "A moderated discussion with multiple panelists" },
            new EventSessionKind { Id = (int)EventSessionKindEnum.Lecture, MasterCode = "LECTURE", FullName = "Lecture", Description = "A formal instructional presentation" },
            new EventSessionKind { Id = (int)EventSessionKindEnum.Class, MasterCode = "CLASS", FullName = "Class", Description = "A structured learning session" },
            new EventSessionKind { Id = (int)EventSessionKindEnum.Activity, MasterCode = "ACTIVITY", FullName = "Activity", Description = "An activity or participatory program item" },
            new EventSessionKind { Id = (int)EventSessionKindEnum.Keynote, MasterCode = "KEYNOTE", FullName = "Keynote", Description = "A featured keynote session" },
            new EventSessionKind { Id = (int)EventSessionKindEnum.LightningTalk, MasterCode = "LIGHTNING_TALK", FullName = "Lightning talk", Description = "A short, focused presentation" },
            new EventSessionKind { Id = (int)EventSessionKindEnum.BOF, MasterCode = "BOF", FullName = "Birds of a feather", Description = "An informal discussion around a shared topic" },
            new EventSessionKind { Id = (int)EventSessionKindEnum.Demo, MasterCode = "DEMO", FullName = "Demo", Description = "A demonstration or showcase" },
            new EventSessionKind { Id = (int)EventSessionKindEnum.QAndA, MasterCode = "Q_AND_A", FullName = "Q&A", Description = "A question-and-answer session" },
            new EventSessionKind { Id = (int)EventSessionKindEnum.Other, MasterCode = "OTHER", FullName = "Other", Description = "A program item not covered by standard kinds" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedActorTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<ActorType>().AnyAsync(ct)) return;

        context.Set<ActorType>().AddRange(
            new ActorType { Id = (int)ActorTypeEnum.User, MasterCode = "USER", FullName = "User", Description = "Individual user actor" },
            new ActorType { Id = (int)ActorTypeEnum.Organization, MasterCode = "ORGANIZATION", FullName = "Organization", Description = "Organization actor" },
            new ActorType { Id = (int)ActorTypeEnum.Bot, MasterCode = "BOT", FullName = "Bot", Description = "Automated bot actor" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedActorSubscriptionStatusesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<ActorSubscriptionStatus>().AnyAsync(ct)) return;

        context.Set<ActorSubscriptionStatus>().AddRange(
            new ActorSubscriptionStatus { Id = (int)ActorSubscriptionStatusEnum.Active, MasterCode = "ACTIVE", FullName = "Active", Description = "Subscriber receives fanout notifications for the target actor" },
            new ActorSubscriptionStatus { Id = (int)ActorSubscriptionStatusEnum.Unsubscribed, MasterCode = "UNSUBSCRIBED", FullName = "Unsubscribed", Description = "Subscriber explicitly opted out while preserving history" },
            new ActorSubscriptionStatus { Id = (int)ActorSubscriptionStatusEnum.Blocked, MasterCode = "BLOCKED", FullName = "Blocked", Description = "Subscription is administratively blocked" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedActorSubscriptionNotificationLevelsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<ActorSubscriptionNotificationLevel>().AnyAsync(ct)) return;

        context.Set<ActorSubscriptionNotificationLevel>().AddRange(
            new ActorSubscriptionNotificationLevel { Id = (int)ActorSubscriptionNotificationLevelEnum.None, MasterCode = "NONE", FullName = "None", Description = "No notifications are generated for this subscription" },
            new ActorSubscriptionNotificationLevel { Id = (int)ActorSubscriptionNotificationLevelEnum.All, MasterCode = "ALL", FullName = "All", Description = "All V1 fanout notifications are generated for this subscription" },
            new ActorSubscriptionNotificationLevel { Id = (int)ActorSubscriptionNotificationLevelEnum.Personalized, MasterCode = "PERSONALIZED", FullName = "Personalized", Description = "Future personalized fanout policy placeholder" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedRoleScopesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<RoleScope>().AnyAsync(ct)) return;

        context.Set<RoleScope>().AddRange(
            new RoleScope { Id = (int)RoleScopeEnum.Platform, MasterCode = "PLATFORM", FullName = "Platform", Description = "Platform-wide roles and permissions" },
            new RoleScope { Id = (int)RoleScopeEnum.Tenant, MasterCode = "TENANT", FullName = "Tenant", Description = "Tenant-scoped roles and permissions" },
            new RoleScope { Id = (int)RoleScopeEnum.Organization, MasterCode = "ORGANIZATION", FullName = "Organization", Description = "Organization-scoped roles and permissions" },
            new RoleScope { Id = (int)RoleScopeEnum.Group, MasterCode = "GROUP", FullName = "Group", Description = "Group-scoped roles and permissions" },
            new RoleScope { Id = (int)RoleScopeEnum.Event, MasterCode = "EVENT", FullName = "Event", Description = "Event-scoped roles and permissions" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedSettingScopesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<SettingScopeLookup>().AnyAsync(ct)) return;

        context.Set<SettingScopeLookup>().AddRange(
            new SettingScopeLookup { Id = (int)ConfigurationScopeEnum.System, MasterCode = "SYSTEM", FullName = "System", Description = "Global system configuration scope" },
            new SettingScopeLookup { Id = (int)ConfigurationScopeEnum.Instance, MasterCode = "INSTANCE", FullName = "Instance", Description = "Application instance configuration scope" },
            new SettingScopeLookup { Id = (int)ConfigurationScopeEnum.Tenant, MasterCode = "TENANT", FullName = "Tenant", Description = "Tenant configuration scope" },
            new SettingScopeLookup { Id = (int)ConfigurationScopeEnum.Organization, MasterCode = "ORGANIZATION", FullName = "Organization", Description = "Organization configuration scope" },
            new SettingScopeLookup { Id = (int)ConfigurationScopeEnum.Group, MasterCode = "GROUP", FullName = "Group", Description = "Group configuration scope" },
            new SettingScopeLookup { Id = (int)ConfigurationScopeEnum.User, MasterCode = "USER", FullName = "User", Description = "User configuration scope" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedSettingValueTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        var expected = new[]
        {
            new SettingValueTypeLookup { Id = (int)SettingValueType.String, MasterCode = "STRING", FullName = "String", Description = "String setting value" },
            new SettingValueTypeLookup { Id = (int)SettingValueType.Integer, MasterCode = "INTEGER", FullName = "Integer", Description = "Integer setting value" },
            new SettingValueTypeLookup { Id = (int)SettingValueType.Boolean, MasterCode = "BOOLEAN", FullName = "Boolean", Description = "Boolean setting value" },
            new SettingValueTypeLookup { Id = (int)SettingValueType.Decimal, MasterCode = "DECIMAL", FullName = "Decimal", Description = "Decimal setting value" },
            new SettingValueTypeLookup { Id = (int)SettingValueType.Json, MasterCode = "JSON", FullName = "JSON", Description = "JSON setting value" },
            new SettingValueTypeLookup { Id = (int)SettingValueType.DateTime, MasterCode = "DATE_TIME", FullName = "Date/Time", Description = "Date/time setting value" },
            new SettingValueTypeLookup { Id = (int)SettingValueType.Long, MasterCode = "LONG", FullName = "Long Integer", Description = "64-bit integer setting value" }
        };

        var existingIds = await context.Set<SettingValueTypeLookup>()
            .AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync(ct);
        var existingIdSet = existingIds.ToHashSet();
        var missing = expected.Where(x => !existingIdSet.Contains(x.Id)).ToList();

        if (missing.Count == 0) return;

        context.Set<SettingValueTypeLookup>().AddRange(missing);
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedSecretSourceTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<SecretSourceTypeLookup>().AnyAsync(ct)) return;

        context.Set<SecretSourceTypeLookup>().AddRange(
            new SecretSourceTypeLookup { Id = (int)SecretSourceType.Infisical, MasterCode = "INFISICAL", FullName = "Infisical", Description = "Secret value is stored in Infisical" },
            new SecretSourceTypeLookup { Id = (int)SecretSourceType.InlineEncrypted, MasterCode = "INLINE_ENCRYPTED", FullName = "Inline Encrypted", Description = "Secret value is stored encrypted in the database" },
            new SecretSourceTypeLookup { Id = (int)SecretSourceType.EnvironmentVariable, MasterCode = "ENVIRONMENT_VARIABLE", FullName = "Environment Variable", Description = "Secret value is resolved from an environment variable" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedSecretValidationStatusesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<SecretValidationStatus>().AnyAsync(ct)) return;

        context.Set<SecretValidationStatus>().AddRange(
            new SecretValidationStatus { Id = (int)SecretValidationResult.NotValidated, MasterCode = "NOT_VALIDATED", FullName = "Not Validated", Description = "Secret source has not been validated" },
            new SecretValidationStatus { Id = (int)SecretValidationResult.Success, MasterCode = "SUCCESS", FullName = "Success", Description = "Secret source validation succeeded" },
            new SecretValidationStatus { Id = (int)SecretValidationResult.Failure, MasterCode = "FAILURE", FullName = "Failure", Description = "Secret source validation failed" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedExternalApiKeyOwnerTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<ExternalApiKeyOwnerTypeLookup>().AnyAsync(ct)) return;

        context.Set<ExternalApiKeyOwnerTypeLookup>().AddRange(
            new ExternalApiKeyOwnerTypeLookup { Id = (int)ExternalApiKeyOwnerType.User, MasterCode = "USER", FullName = "User", Description = "External API key owned by a user" },
            new ExternalApiKeyOwnerTypeLookup { Id = (int)ExternalApiKeyOwnerType.Organization, MasterCode = "ORGANIZATION", FullName = "Organization", Description = "External API key owned by an organization" },
            new ExternalApiKeyOwnerTypeLookup { Id = (int)ExternalApiKeyOwnerType.Group, MasterCode = "GROUP", FullName = "Group", Description = "External API key owned by a group" },
            new ExternalApiKeyOwnerTypeLookup { Id = (int)ExternalApiKeyOwnerType.Tenant, MasterCode = "TENANT", FullName = "Tenant", Description = "External API key owned by a tenant" },
            new ExternalApiKeyOwnerTypeLookup { Id = (int)ExternalApiKeyOwnerType.InstanceAdmin, MasterCode = "INSTANCE_ADMIN", FullName = "Instance Admin", Description = "External API key owned by an instance administrator" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedNotificationScopeTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<NotificationScopeType>().AnyAsync(ct)) return;

        context.Set<NotificationScopeType>().AddRange(
            new NotificationScopeType { Id = (int)ActorTypeEnum.User, MasterCode = "USER", FullName = "User", Description = "Notification targets a single user" },
            new NotificationScopeType { Id = (int)ActorTypeEnum.Organization, MasterCode = "ORGANIZATION", FullName = "Organization", Description = "Notification targets an organization scope" },
            new NotificationScopeType { Id = (int)ActorTypeEnum.Group, MasterCode = "GROUP", FullName = "Group", Description = "Notification targets a group scope" },
            new NotificationScopeType { Id = (int)ActorTypeEnum.System, MasterCode = "SYSTEM", FullName = "System", Description = "Notification targets a system scope" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedApprovalStatusesAsync(ExploreDbContext context, CancellationToken ct)
    {
        var existingIds = await context.Set<ApprovalStatus>()
            .Select(status => status.Id)
            .ToListAsync(ct);
        var missingStatuses = new[]
        {
            new ApprovalStatus { Id = (int)ApprovalStatusEnum.Pending, MasterCode = "PENDING", FullName = "Pending", Description = "Status is pending approval of Admin verifying the Existence of Legal Entity" },
            new ApprovalStatus { Id = (int)ApprovalStatusEnum.Approved, MasterCode = "APPROVED", FullName = "Approved", Description = "Status has been approved by Admin after verifying the Existence of Legal Entity" },
            new ApprovalStatus { Id = (int)ApprovalStatusEnum.Rejected, MasterCode = "REJECTED", FullName = "Rejected", Description = "Status has been rejected by Admin after failing to verify the Existence of Legal Entity" },
            new ApprovalStatus { Id = (int)ApprovalStatusEnum.Waitlisted, MasterCode = "WAITLISTED", FullName = "Waitlisted", Description = "Registration is waitlisted because the event session is currently at capacity" }
        }.Where(status => !existingIds.Contains(status.Id));

        context.Set<ApprovalStatus>().AddRange(missingStatuses);
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedAnalyticsProvidersAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<AnalyticsProvider>().AnyAsync(ct)) return;

        context.Set<AnalyticsProvider>().AddRange(
            new AnalyticsProvider { Id = (int)AnalyticsProviderEnum.None, MasterCode = "NONE", FullName = "None", Description = "Analytics disabled" },
            new AnalyticsProvider { Id = (int)AnalyticsProviderEnum.Posthog, MasterCode = "POSTHOG", FullName = "PostHog", Description = "PostHog analytics provider" },
            new AnalyticsProvider { Id = (int)AnalyticsProviderEnum.Plausible, MasterCode = "PLAUSIBLE", FullName = "Plausible", Description = "Plausible analytics provider" },
            new AnalyticsProvider { Id = (int)AnalyticsProviderEnum.Rybbit, MasterCode = "RYBBIT", FullName = "Rybbit", Description = "Rybbit analytics provider" },
            new AnalyticsProvider { Id = (int)AnalyticsProviderEnum.RudderStack, MasterCode = "RUDDERSTACK", FullName = "RudderStack", Description = "RudderStack analytics provider" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedTenantStatusesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<TenantStatus>().AnyAsync(ct)) return;

        context.Set<TenantStatus>().AddRange(
            new TenantStatus { Id = (int)TenantStatusEnum.Provisioning, MasterCode = "PROVISIONING", FullName = "Provisioning", Description = "Tenant is being set up", IsActiveState = false },
            new TenantStatus { Id = (int)TenantStatusEnum.Active, MasterCode = "ACTIVE", FullName = "Active", Description = "Tenant is active and operational", IsActiveState = true },
            new TenantStatus { Id = (int)TenantStatusEnum.Suspended, MasterCode = "SUSPENDED", FullName = "Suspended", Description = "Tenant is temporarily suspended", IsActiveState = false },
            new TenantStatus { Id = (int)TenantStatusEnum.Archived, MasterCode = "ARCHIVED", FullName = "Archived", Description = "Tenant is archived and read-only", IsActiveState = false },
            new TenantStatus { Id = (int)TenantStatusEnum.Purged, MasterCode = "PURGED", FullName = "Purged", Description = "Tenant data has been permanently removed", IsActiveState = false });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedAudienceAgesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<AudienceAge>().AnyAsync(ct)) return;

        context.Set<AudienceAge>().AddRange(
            new AudienceAge { Id = (int)AudienceAgeEnum.AllAges, MasterCode = "ALL_AGES", FullName = "All Ages", MinAge = null, MaxAge = null },
            new AudienceAge { Id = (int)AudienceAgeEnum.AdultsOnly18Plus, MasterCode = "ADULTS_18_PLUS", FullName = "Adults Only (18+)", MinAge = 18, MaxAge = null },
            new AudienceAge { Id = (int)AudienceAgeEnum.Teens16Plus, MasterCode = "TEENS_16_PLUS", FullName = "Teens & Adults (16+)", MinAge = 16, MaxAge = null },
            new AudienceAge { Id = (int)AudienceAgeEnum.Preteens12Plus, MasterCode = "PRETEENS_12_PLUS", FullName = "Preteens & Up (12+)", MinAge = 12, MaxAge = null },
            new AudienceAge { Id = (int)AudienceAgeEnum.ChildrenUnder6, MasterCode = "CHILDREN_UNDER_6", FullName = "Young Children (0-6)", MinAge = null, MaxAge = 6 },
            new AudienceAge { Id = (int)AudienceAgeEnum.YouthUnder12, MasterCode = "YOUTH_UNDER_12", FullName = "Children (0-12)", MinAge = null, MaxAge = 12 },
            new AudienceAge { Id = (int)AudienceAgeEnum.YouthUnder16, MasterCode = "YOUTH_UNDER_16", FullName = "Children & Young Teens (0-16)", MinAge = null, MaxAge = 16 },
            new AudienceAge { Id = (int)AudienceAgeEnum.YouthUnder18, MasterCode = "YOUTH_UNDER_18", FullName = "Youth (0-18)", MinAge = null, MaxAge = 18 });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedAudienceGendersAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<AudienceGender>().AnyAsync(ct)) return;

        context.Set<AudienceGender>().AddRange(
            new AudienceGender { Id = (int)AudienceGenderEnum.Man, MasterCode = "MAN", FullName = "Man", Description = "Only for Man Audience" },
            new AudienceGender { Id = (int)AudienceGenderEnum.Woman, MasterCode = "WOMAN", FullName = "Woman", Description = "Only for Woman Audience" },
            new AudienceGender { Id = (int)AudienceGenderEnum.Both, MasterCode = "BOTH_SEGREGATED", FullName = "Both Segregated", Description = "For Both Man and Woman but Segregated so no free mixing" },
            new AudienceGender { Id = 4, MasterCode = "BOTH_FREE_MIXING", FullName = "Both Free Mixing", Description = "For Both Man and Woman but Free Mixing" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedDidCustodyTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<DidCustodyType>().AnyAsync(ct)) return;

        context.Set<DidCustodyType>().AddRange(
            new DidCustodyType { Id = (int)DidCustodyTypeEnum.Custodial, MasterCode = "CUSTODIAL", FullName = "Custodial", Description = "Platform manages the DID keys" },
            new DidCustodyType { Id = (int)DidCustodyTypeEnum.SelfCustody, MasterCode = "SELF_CUSTODY", FullName = "Self-Custody", Description = "User manages their own DID keys" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedEventFormatsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<EventFormat>().AnyAsync(ct)) return;

        context.Set<EventFormat>().AddRange(
            new EventFormat { Id = (int)EventFormatEnum.Local, MasterCode = "LOCAL", FullName = "Local (In-Person)", Description = "Event takes place at a physical location" },
            new EventFormat { Id = (int)EventFormatEnum.Digital, MasterCode = "DIGITAL", FullName = "Digital (Online)", Description = "Event takes place online" },
            new EventFormat { Id = (int)EventFormatEnum.Hybrid, MasterCode = "HYBRID", FullName = "Hybrid", Description = "Event takes place both in-person and online" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedEventStatusesAsync(ExploreDbContext context, CancellationToken ct)
    {
        var statuses = new EventStatus[]
        {
            new EventStatus { Id = (int)EventStatusEnum.Draft, MasterCode = "DRAFT", FullName = "Draft", Description = "Event is in draft state and not visible to the public" },
            new EventStatus { Id = (int)EventStatusEnum.Published, MasterCode = "PUBLISHED", FullName = "Published", Description = "Event is published and visible to the public" },
            new EventStatus { Id = (int)EventStatusEnum.Cancelled, MasterCode = "CANCELLED", FullName = "Cancelled", Description = "Event has been cancelled" },
            new EventStatus { Id = (int)EventStatusEnum.Completed, MasterCode = "COMPLETED", FullName = "Completed", Description = "Event has been completed" },
            new EventStatus { Id = (int)EventStatusEnum.Archived, MasterCode = "ARCHIVED", FullName = "Archived", Description = "Event has been archived" },
            new EventStatus { Id = (int)EventStatusEnum.Moderated, MasterCode = "MODERATED", FullName = "Moderated", Description = "Event was hidden by administration after moderation" }
        };

        var existingIds = await context.Set<EventStatus>()
            .Select(status => status.Id)
            .ToListAsync(ct);
        var existingIdSet = existingIds.ToHashSet();
        var missingStatuses = statuses
            .Where(status => !existingIdSet.Contains(status.Id))
            .ToArray();

        if (missingStatuses.Length == 0) return;

        context.Set<EventStatus>().AddRange(missingStatuses);
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedEventSessionStatusesAsync(ExploreDbContext context, CancellationToken ct)
    {
        var statuses = new EventSessionStatus[]
        {
            new EventSessionStatus { Id = (int)EventSessionStatusEnum.Draft, MasterCode = "DRAFT", FullName = "Draft", Description = "Session is in draft state and not visible to the public" },
            new EventSessionStatus { Id = (int)EventSessionStatusEnum.Submitted, MasterCode = "SUBMITTED", FullName = "Submitted", Description = "Session has been submitted for review" },
            new EventSessionStatus { Id = (int)EventSessionStatusEnum.UnderReview, MasterCode = "UNDER_REVIEW", FullName = "Under review", Description = "Session is currently being reviewed" },
            new EventSessionStatus { Id = (int)EventSessionStatusEnum.Approved, MasterCode = "APPROVED", FullName = "Approved", Description = "Session has been approved but is not yet published" },
            new EventSessionStatus { Id = (int)EventSessionStatusEnum.Published, MasterCode = "PUBLISHED", FullName = "Published", Description = "Session is published and visible to the public" },
            new EventSessionStatus { Id = (int)EventSessionStatusEnum.Rejected, MasterCode = "REJECTED", FullName = "Rejected", Description = "Session was rejected during review" },
            new EventSessionStatus { Id = (int)EventSessionStatusEnum.Cancelled, MasterCode = "CANCELLED", FullName = "Cancelled", Description = "Session has been cancelled" },
            new EventSessionStatus { Id = (int)EventSessionStatusEnum.Archived, MasterCode = "ARCHIVED", FullName = "Archived", Description = "Session has been archived" },
            new EventSessionStatus { Id = (int)EventSessionStatusEnum.Completed, MasterCode = "COMPLETED", FullName = "Completed", Description = "Session has been completed" },
            new EventSessionStatus { Id = (int)EventSessionStatusEnum.Moderated, MasterCode = "MODERATED", FullName = "Moderated", Description = "Session was hidden by event-level moderation" }
        };

        var existingIds = await context.Set<EventSessionStatus>()
            .Select(status => status.Id)
            .ToListAsync(ct);
        var existingIdSet = existingIds.ToHashSet();
        var missingStatuses = statuses
            .Where(status => !existingIdSet.Contains(status.Id))
            .ToArray();

        if (missingStatuses.Length == 0) return;

        context.Set<EventSessionStatus>().AddRange(missingStatuses);
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedEventTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<EventType>().AnyAsync(ct)) return;

        context.Set<EventType>().AddRange(
            new EventType { Id = (int)EventTypeEnum.Conference, MasterCode = "CONFERENCE", FullName = "Conference" },
            new EventType { Id = (int)EventTypeEnum.Webinar, MasterCode = "WEBINAR", FullName = "Webinar" },
            new EventType { Id = (int)EventTypeEnum.Workshop, MasterCode = "WORKSHOP", FullName = "Workshop" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedFileTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<FileType>().AnyAsync(ct)) return;

        context.Set<FileType>().AddRange(
            new FileType { Id = (int)FileTypeEnum.Image, MasterCode = "IMAGE", FullName = "Image", Description = "Image file (PNG, JPG, GIF, etc.)" },
            new FileType { Id = (int)FileTypeEnum.Document, MasterCode = "DOCUMENT", FullName = "Document", Description = "Document file (PDF, DOC, etc.)" },
            new FileType { Id = (int)FileTypeEnum.Video, MasterCode = "VIDEO", FullName = "Video", Description = "Video file (MP4, AVI, etc.)" },
            new FileType { Id = (int)FileTypeEnum.Audio, MasterCode = "AUDIO", FullName = "Audio", Description = "Audio file (MP3, WAV, etc.)" },
            new FileType { Id = (int)FileTypeEnum.Other, MasterCode = "OTHER", FullName = "Other", Description = "Other file type" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedLanguagesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<Language>().AnyAsync(ct)) return;

        context.Set<Language>().AddRange(
            new Language { Id = 1, MasterCode = "AR", FullName = "Arabic", Description = "Arabic language" },
            new Language { Id = 2, MasterCode = "EN", FullName = "English", Description = "English language" },
            new Language { Id = 3, MasterCode = "FR", FullName = "French", Description = "French language" },
            new Language { Id = 4, MasterCode = "TR", FullName = "Turkish", Description = "Turkish language" },
            new Language { Id = 5, MasterCode = "UR", FullName = "Urdu", Description = "Urdu language" },
            new Language { Id = 6, MasterCode = "ID", FullName = "Indonesian", Description = "Indonesian language" },
            new Language { Id = 7, MasterCode = "MS", FullName = "Malay", Description = "Malay language" },
            new Language { Id = 8, MasterCode = "BN", FullName = "Bengali", Description = "Bengali language" },
            new Language { Id = 9, MasterCode = "FA", FullName = "Persian", Description = "Persian/Farsi language" },
            new Language { Id = 10, MasterCode = "DE", FullName = "German", Description = "German language" },
            new Language { Id = 11, MasterCode = "NL", FullName = "Dutch", Description = "Dutch language" },
            new Language { Id = 12, MasterCode = "ES", FullName = "Spanish", Description = "Spanish language" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedMadhabsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<Madhab>().AnyAsync(ct)) return;

        context.Set<Madhab>().AddRange(
            new Madhab { Id = (int)MadhabEnum.Hanafi, MasterCode = "HANAFI", FullName = "Hanafi", Description = "Hanafi school of Islamic jurisprudence" },
            new Madhab { Id = (int)MadhabEnum.Maliki, MasterCode = "MALIKI", FullName = "Maliki", Description = "Maliki school of Islamic jurisprudence" },
            new Madhab { Id = (int)MadhabEnum.Shafii, MasterCode = "SHAFII", FullName = "Shafi'i", Description = "Shafi'i school of Islamic jurisprudence" },
            new Madhab { Id = (int)MadhabEnum.Hanbali, MasterCode = "HANBALI", FullName = "Hanbali", Description = "Hanbali school of Islamic jurisprudence" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedModuleDefinitionsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<ModuleDefinition>().AnyAsync(ct)) return;

        var seedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        context.Set<ModuleDefinition>().AddRange(
            new ModuleDefinition { Id = SeedIds.ModuleCoreId, ModuleKey = "Mod_Core", Name = "Core Events", Description = "Basic event functionality - title, description, sessions, locations", IconName = "Event", Category = "Core", DisplayOrder = 0, IsActive = true, CreatedAt = seedTimestamp },
            new ModuleDefinition { Id = SeedIds.ModuleIslamicId, ModuleKey = "Mod_Islamic", Name = "Islamic Events", Description = "Islamic-specific features: Madhab selection, prayer time scheduling, gender segregation", IconName = "Mosque", Category = "Domain", DisplayOrder = 1, IsActive = true, CreatedAt = seedTimestamp },
            new ModuleDefinition { Id = SeedIds.ModuleTechId, ModuleKey = "Mod_Tech", Name = "Tech Events", Description = "Developer event features: GitHub repositories, skill levels, live coding sessions", IconName = "Code", Category = "Domain", DisplayOrder = 2, IsActive = true, CreatedAt = seedTimestamp });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedOrganizationPositionsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<OrganizationPosition>().AnyAsync(ct)) return;

        context.Set<OrganizationPosition>().AddRange(
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Founder, MasterCode = "FOUNDER", FullName = "Founder", Description = "Organization founder" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Director, MasterCode = "DIRECTOR", FullName = "Director", Description = "Organization director" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Manager, MasterCode = "MANAGER", FullName = "Manager", Description = "Organization manager" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Teacher, MasterCode = "TEACHER", FullName = "Teacher", Description = "Teacher or instructor" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Secretary, MasterCode = "SECRETARY", FullName = "Secretary", Description = "Organization secretary" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Treasurer, MasterCode = "TREASURER", FullName = "Treasurer", Description = "Organization treasurer" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Coordinator, MasterCode = "COORDINATOR", FullName = "Coordinator", Description = "Event or activity coordinator" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Volunteer, MasterCode = "VOLUNTEER", FullName = "Volunteer", Description = "Organization volunteer" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Intern, MasterCode = "INTERN", FullName = "Intern", Description = "Organization intern" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Advisor, MasterCode = "ADVISOR", FullName = "Advisor", Description = "Organization advisor" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Consultant, MasterCode = "CONSULTANT", FullName = "Consultant", Description = "Organization consultant" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Supervisor, MasterCode = "SUPERVISOR", FullName = "Supervisor", Description = "Supervisor" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Assistant, MasterCode = "ASSISTANT", FullName = "Assistant", Description = "Assistant" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Staff, MasterCode = "STAFF", FullName = "Staff", Description = "General staff member" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedGroupPositionsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<GroupPosition>().AnyAsync(ct)) return;

        context.Set<GroupPosition>().AddRange(
            new GroupPosition { Id = (int)GroupPositionEnum.Leader, MasterCode = "LEADER", FullName = "Leader", Description = "Group leader" },
            new GroupPosition { Id = (int)GroupPositionEnum.CoLeader, MasterCode = "CO_LEADER", FullName = "Co-Leader", Description = "Group co-leader" },
            new GroupPosition { Id = (int)GroupPositionEnum.Coordinator, MasterCode = "COORDINATOR", FullName = "Coordinator", Description = "Group coordinator" },
            new GroupPosition { Id = (int)GroupPositionEnum.Moderator, MasterCode = "MODERATOR", FullName = "Moderator", Description = "Group moderator" },
            new GroupPosition { Id = (int)GroupPositionEnum.Secretary, MasterCode = "SECRETARY", FullName = "Secretary", Description = "Group secretary" },
            new GroupPosition { Id = (int)GroupPositionEnum.Treasurer, MasterCode = "TREASURER", FullName = "Treasurer", Description = "Group treasurer" },
            new GroupPosition { Id = (int)GroupPositionEnum.Mentor, MasterCode = "MENTOR", FullName = "Mentor", Description = "Group mentor" },
            new GroupPosition { Id = (int)GroupPositionEnum.Facilitator, MasterCode = "FACILITATOR", FullName = "Facilitator", Description = "Group facilitator" },
            new GroupPosition { Id = (int)GroupPositionEnum.Volunteer, MasterCode = "VOLUNTEER", FullName = "Volunteer", Description = "Group volunteer" },
            new GroupPosition { Id = (int)GroupPositionEnum.Member, MasterCode = "MEMBER", FullName = "Member", Description = "General group member" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedRegistrationModesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<RegistrationMode>().AnyAsync(ct)) return;

        context.Set<RegistrationMode>().AddRange(
            new RegistrationMode { Id = (int)RegistrationModeEnum.Open, MasterCode = "OPEN", FullName = "Open", Description = "Anyone can register" },
            new RegistrationMode { Id = (int)RegistrationModeEnum.ApprovalRequired, MasterCode = "APPROVAL_REQUIRED", FullName = "Approval Required", Description = "Registration requires approval" },
            new RegistrationMode { Id = (int)RegistrationModeEnum.InviteOnly, MasterCode = "INVITE_ONLY", FullName = "Invite Only", Description = "Only invited users can register" },
            new RegistrationMode { Id = (int)RegistrationModeEnum.Closed, MasterCode = "CLOSED", FullName = "Closed", Description = "Registration is closed" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedSystemSettingsAsync(ExploreDbContext context, CancellationToken ct)
    {
        var seedTimestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedSettings = new[]
        {
            new SystemSetting { Id = SeedIds.SystemSettingDeploymentModeId, SettingKey = GovernanceSettingKeys.Deployment.Mode, Value = "\"SingleTenant\"", ValueType = SettingValueType.String, IsLocked = true, AllowedValues = "[\"SingleTenant\", \"MultiTenant\"]", Description = "Deployment mode of the application", Category = "System", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingMaxSessionsPerEventId, SettingKey = "events.max_sessions_per_event", Value = "100", ValueType = SettingValueType.Integer, IsLocked = false, Description = "Maximum number of sessions allowed per event", Category = "Events", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingRequireApprovalId, SettingKey = "events.require_approval", Value = "false", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether events require admin approval before publishing", Category = "Events", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingIslamicModuleId, SettingKey = GovernanceSettingKeys.Modules.IslamicEnabled, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Enable Islamic event module", Category = "Modules", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingTechModuleId, SettingKey = GovernanceSettingKeys.Modules.TechEnabled, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Enable Tech event module", Category = "Modules", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingTenantSelfServiceRegistrationId, SettingKey = GovernanceSettingKeys.Tenants.SelfServiceRegistration, Value = "false", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether tenants can self-register without manual instance admin invitation", Category = "Tenant", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingTenantWhiteLabelingEnabledId, SettingKey = GovernanceSettingKeys.Tenants.WhiteLabelingEnabled, Value = "false", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether tenant-level white-label branding overrides are enabled in multi-tenant mode", Category = "Tenant", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingRoutingDefaultPublicHomePageId, SettingKey = GovernanceSettingKeys.Routing.DefaultPublicHomePage, Value = "\"EventList\"", ValueType = SettingValueType.String, IsLocked = false, AllowedValues = "[\"EventList\", \"LandingPage\"]", Description = "Default public home page for tenants", Category = "Routing", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingUserSubmissionEnabledId, SettingKey = GovernanceSettingKeys.Events.UserSubmissionEnabled, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether tenant users are allowed to submit events", Category = "Events", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingOrganizationVerificationRequiredId, SettingKey = GovernanceSettingKeys.Organizations.VerificationRequired, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether organization verification is required before organizations can operate", Category = "Organizations", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingOrganizationTenantCanOmitVerificationId, SettingKey = GovernanceSettingKeys.Organizations.TenantCanOmitVerification, Value = "false", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether tenant administrators may omit organization verification requirements", Category = "Organizations", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingOrgSubmissionEnabledId, SettingKey = GovernanceSettingKeys.Events.OrganizationSubmissionEnabled, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether organizations are allowed to submit events", Category = "Events", DisplayOrder = 4, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingGroupSubmissionEnabledId, SettingKey = GovernanceSettingKeys.Events.GroupSubmissionEnabled, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether groups are allowed to submit events", Category = "Events", DisplayOrder = 5, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingOrgSelfRegistrationEnabledId, SettingKey = GovernanceSettingKeys.Organizations.SelfRegistrationEnabled, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether users can self-register organizations", Category = "Organizations", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingGroupSelfRegistrationEnabledId, SettingKey = GovernanceSettingKeys.Groups.SelfRegistrationEnabled, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether users can self-register groups", Category = "Groups", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingDomainsInstanceBaseDomainId, SettingKey = GovernanceSettingKeys.Domains.InstanceBaseDomain, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Instance base domain used for tenant subdomain generation", Category = "Domains", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingDomainsAllowTenantCustomDomainId, SettingKey = GovernanceSettingKeys.Domains.AllowTenantCustomDomain, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether tenant administrators can configure custom domains", Category = "Domains", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingDomainsTenantSubdomainId, SettingKey = GovernanceSettingKeys.Domains.TenantSubdomain, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Tenant subdomain override placeholder", Category = "Domains", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingDomainsTenantCustomDomainId, SettingKey = GovernanceSettingKeys.Domains.TenantCustomDomain, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Tenant custom domain override placeholder", Category = "Domains", DisplayOrder = 4, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingBrandingDisplayNameId, SettingKey = GovernanceSettingKeys.Branding.DisplayName, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Default brand display name shown when tenants do not override branding", Category = "Branding", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingBrandingLogoUrlId, SettingKey = GovernanceSettingKeys.Branding.LogoUrl, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Default logo URL shown when tenants do not override branding", Category = "Branding", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingBrandingFaviconUrlId, SettingKey = GovernanceSettingKeys.Branding.FaviconUrl, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Default favicon URL shown when tenants do not override branding", Category = "Branding", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingBrandingCustomCssUrlId, SettingKey = GovernanceSettingKeys.Branding.CustomCssUrl, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Default custom CSS URL applied when tenants do not override branding", Category = "Branding", DisplayOrder = 4, CreatedAt = seedTimestamp },

            // Email / SMTP settings — unlocked by default so tenants can bring their own SMTP
            new SystemSetting { Id = SeedIds.SystemSettingEmailSmtpHostId, SettingKey = GovernanceSettingKeys.Email.SmtpHost, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "SMTP server hostname (e.g., smtp.gmail.com, smtp.mailgun.org)", Category = "Email", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingEmailSmtpPortId, SettingKey = GovernanceSettingKeys.Email.SmtpPort, Value = "587", ValueType = SettingValueType.Integer, IsLocked = false, Description = "SMTP server port (587 for StartTLS, 465 for SSL, 25 for unencrypted)", Category = "Email", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingEmailSmtpUsernameId, SettingKey = InfrastructureSecretSettingKeys.Email.SmtpUsername, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "SMTP authentication username", Category = "Email", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingEmailSmtpPasswordId, SettingKey = InfrastructureSecretSettingKeys.Email.SmtpPassword, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "SMTP authentication password (stored encrypted)", Category = "Email", DisplayOrder = 4, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingEmailSmtpSecurityId, SettingKey = GovernanceSettingKeys.Email.SmtpSecurity, Value = "\"StartTls\"", ValueType = SettingValueType.String, IsLocked = false, AllowedValues = "[\"None\", \"StartTls\", \"SslOnConnect\", \"Auto\"]", Description = "SMTP connection security mode", Category = "Email", DisplayOrder = 5, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingEmailFromAddressId, SettingKey = GovernanceSettingKeys.Email.FromAddress, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Default sender email address for outbound emails", Category = "Email", DisplayOrder = 6, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingEmailFromNameId, SettingKey = GovernanceSettingKeys.Email.FromName, Value = "\"Explore\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Default sender display name for outbound emails", Category = "Email", DisplayOrder = 7, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingEmailSmtpTimeoutId, SettingKey = GovernanceSettingKeys.Email.SmtpTimeoutSeconds, Value = "30", ValueType = SettingValueType.Integer, IsLocked = false, Description = "SMTP connection timeout in seconds", Category = "Email", DisplayOrder = 8, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingEmailSmtpSkipCertValidationId, SettingKey = GovernanceSettingKeys.Email.SmtpSkipCertValidation, Value = "false", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Skip TLS certificate validation (development/self-signed certs only)", Category = "Email", DisplayOrder = 9, CreatedAt = seedTimestamp },

            // Object Storage - local-first provider policy and optional S3 settings
            new SystemSetting { Id = SeedIds.SystemSettingStorageProviderId, SettingKey = GovernanceSettingKeys.Storage.Provider, Value = $"\"{StorageProviders.Local}\"", ValueType = SettingValueType.String, IsLocked = false, AllowedValues = "[\"local\", \"s3_compatible\", \"legacy_external\"]", Description = "Selected storage provider. Local filesystem is the default; S3-compatible storage is optional.", Category = "ObjectStorage", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingStorageDefaultMaxUploadBytesId, SettingKey = GovernanceSettingKeys.Storage.DefaultMaxUploadBytes, Value = "10485760", ValueType = SettingValueType.Long, IsLocked = false, Description = "Default maximum upload size in bytes for tenant storage policy.", Category = "ObjectStorage", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingStorageDefaultTenantQuotaBytesId, SettingKey = GovernanceSettingKeys.Storage.DefaultTenantQuotaBytes, Value = "1073741824", ValueType = SettingValueType.Long, IsLocked = false, Description = "Default tenant storage quota in bytes.", Category = "ObjectStorage", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingStorageInstanceMaxUploadBytesId, SettingKey = GovernanceSettingKeys.Storage.InstanceMaxUploadBytes, Value = "104857600", ValueType = SettingValueType.Long, IsLocked = true, Description = "Instance-wide upload ceiling in bytes; tenant overrides cannot exceed this value.", Category = "ObjectStorage", DisplayOrder = 4, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingS3EndpointId, SettingKey = GovernanceSettingKeys.Storage.Endpoint, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Optional S3-compatible endpoint URL (e.g., https://fsn1.your-objectstorage.com)", Category = "ObjectStorage", DisplayOrder = 5, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingS3PublicEndpointId, SettingKey = GovernanceSettingKeys.Storage.PublicEndpoint, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Optional public S3 endpoint for presigned URLs (if different from internal endpoint)", Category = "ObjectStorage", DisplayOrder = 6, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingS3BucketNameId, SettingKey = GovernanceSettingKeys.Storage.BucketName, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Optional S3 bucket name for object storage", Category = "ObjectStorage", DisplayOrder = 7, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingS3AccessKeyIdId, SettingKey = InfrastructureSecretSettingKeys.Storage.AccessKeyId, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Optional S3 access key ID for authentication", Category = "ObjectStorage", DisplayOrder = 8, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingS3SecretAccessKeyId, SettingKey = InfrastructureSecretSettingKeys.Storage.SecretAccessKey, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Optional S3 secret access key for authentication (stored encrypted)", Category = "ObjectStorage", DisplayOrder = 9, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingS3RegionId, SettingKey = GovernanceSettingKeys.Storage.Region, Value = "\"fsn1\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Optional S3 region identifier (e.g., fsn1 for Hetzner, us-east-1 for AWS)", Category = "ObjectStorage", DisplayOrder = 10, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingS3ForcePathStyleId, SettingKey = GovernanceSettingKeys.Storage.ForcePathStyle, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Use path-style URLs for optional S3-compatible storage", Category = "ObjectStorage", DisplayOrder = 11, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingS3UploadUrlExpirationMinutesId, SettingKey = GovernanceSettingKeys.Storage.UploadUrlExpirationMinutes, Value = "60", ValueType = SettingValueType.Integer, IsLocked = false, Description = "Optional S3 presigned upload URL expiration time in minutes", Category = "ObjectStorage", DisplayOrder = 12, CreatedAt = seedTimestamp },

            // Analytics
            new SystemSetting { Id = SeedIds.SystemSettingAnalyticsProviderId, SettingKey = GovernanceSettingKeys.Analytics.Provider, Value = "\"none\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Analytics provider (none, posthog, plausible, rybbit, rudderstack)", AllowedValues = "[\"none\",\"posthog\",\"plausible\",\"rybbit\",\"rudderstack\"]", Category = "Analytics", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingAnalyticsEnabledId, SettingKey = GovernanceSettingKeys.Analytics.Enabled, Value = "false", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Enable analytics tracking", Category = "Analytics", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingAnalyticsApiKeyId, SettingKey = GovernanceSettingKeys.Analytics.ApiKey, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Analytics provider public/write API key", Category = "Analytics", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingAnalyticsEndpointUrlId, SettingKey = GovernanceSettingKeys.Analytics.EndpointUrl, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Analytics provider endpoint URL (supports self-hosted deployments)", Category = "Analytics", DisplayOrder = 4, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingAnalyticsPersonalApiKeyId, SettingKey = GovernanceSettingKeys.Analytics.PersonalApiKey, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Personal API key used for analytics feature flag evaluation when supported", Category = "Analytics", DisplayOrder = 5, CreatedAt = seedTimestamp },

            // Localization / TMS
            new SystemSetting { Id = SeedIds.SystemSettingLocalizationDefaultLanguageId, SettingKey = GovernanceSettingKeys.Localization.DefaultLanguage, Value = "\"en\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Default language code (ISO 639-1) for the instance", Category = "Localization", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingLocalizationTmsProviderId, SettingKey = GovernanceSettingKeys.Localization.TmsProvider, Value = "\"none\"", ValueType = SettingValueType.String, IsLocked = false, AllowedValues = "[\"none\",\"tolgee\",\"weblate\"]", Description = "Translation Management System provider (none uses offline bundles)", Category = "Localization", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingLocalizationTmsApiUrlId, SettingKey = GovernanceSettingKeys.Localization.TmsApiUrl, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "TMS API base URL (e.g., https://app.tolgee.io or self-hosted URL)", Category = "Localization", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingLocalizationTmsProjectIdId, SettingKey = GovernanceSettingKeys.Localization.TmsProjectId, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "TMS project identifier", Category = "Localization", DisplayOrder = 4, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingLocalizationTmsComponentId, SettingKey = GovernanceSettingKeys.Localization.TmsComponent, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Weblate component slug (Weblate-specific, leave empty for Tolgee)", Category = "Localization", DisplayOrder = 5, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingLocalizationEnabledLanguagesId, SettingKey = GovernanceSettingKeys.Localization.EnabledLanguages, Value = "\"en,fr,ar\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Comma-separated culture codes the instance has enabled (must be a subset of the compile-time CultureRegistry).", Category = "Localization", DisplayOrder = 6, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingLocalizationFallbackLanguageId, SettingKey = GovernanceSettingKeys.Localization.FallbackLanguage, Value = "\"en\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Fallback language used when a requested translation key is missing; must be in EnabledLanguages.", Category = "Localization", DisplayOrder = 7, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingLocalizationClientPickerEnabledId, SettingKey = GovernanceSettingKeys.Localization.ClientPickerEnabled, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Kill-switch: hides the in-app language picker when false, without a redeploy.", Category = "Localization", DisplayOrder = 8, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingLocalizationForceOfflineModeId, SettingKey = GovernanceSettingKeys.Localization.ForceOfflineMode, Value = "false", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Emergency toggle: routes RuntimeTranslationProvider through OfflineTranslationProvider regardless of tms_provider.", Category = "Localization", DisplayOrder = 9, CreatedAt = seedTimestamp }
        };

        var existingIds = await context.Set<SystemSetting>()
            .AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync(ct);

        var existingIdSet = existingIds.ToHashSet();
        var missingSettings = expectedSettings
            .Where(x => !existingIdSet.Contains(x.Id))
            .ToList();

        if (missingSettings.Count == 0)
        {
            return;
        }

        context.Set<SystemSetting>().AddRange(missingSettings);
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedTagTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<TagType>().AnyAsync(ct)) return;

        context.Set<TagType>().AddRange(
            new TagType { Id = 1, MasterCode = "TITLE", FullName = "Title", Description = "Title-based tags for labeling and categorization" },
            new TagType { Id = 2, MasterCode = "PEOPLE", FullName = "People", Description = "People-based tags for associating persons with content" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedVisibilityTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<VisibilityType>().AnyAsync(ct)) return;

        context.Set<VisibilityType>().AddRange(
            new VisibilityType { Id = (int)VisibilityTypeEnum.Public, MasterCode = "PUBLIC", FullName = "Public", Description = "Visible to everyone" },
            new VisibilityType { Id = (int)VisibilityTypeEnum.Private, MasterCode = "PRIVATE", FullName = "Private", Description = "Only visible to invited members" },
            new VisibilityType { Id = (int)VisibilityTypeEnum.Unlisted, MasterCode = "UNLISTED", FullName = "Unlisted", Description = "Not listed publicly but accessible via direct link" },
            new VisibilityType { Id = (int)VisibilityTypeEnum.MembersOnly, MasterCode = "MEMBERS_ONLY", FullName = "Members Only", Description = "Only visible to organization members" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedRolesAsync(ExploreDbContext context, CancellationToken ct)
    {
        var expectedRoles = new[]
        {
            // Platform scope (1-9)
            new Role { Id = (int)RoleEnum.Admin, MasterCode = "platform.admin", FullName = "Admin", Description = "Platform administration", Scope = RoleScopeEnum.Platform, IsSystem = true },
            new Role { Id = (int)RoleEnum.Moderator, MasterCode = "platform.moderator", FullName = "Moderator", Description = "Platform moderation", Scope = RoleScopeEnum.Platform, IsSystem = true },
            new Role { Id = (int)RoleEnum.Member, MasterCode = "platform.member", FullName = "Member", Description = "Platform member", Scope = RoleScopeEnum.Platform, IsSystem = true },

            // Tenant scope (10-19)
            new Role { Id = (int)RoleEnum.TenantAdmin, MasterCode = "tenant.admin", FullName = "Admin", Description = "Tenant administration", Scope = RoleScopeEnum.Tenant, IsSystem = true },
            new Role { Id = (int)RoleEnum.TenantModerator, MasterCode = "tenant.moderator", FullName = "Moderator", Description = "Tenant content moderation", Scope = RoleScopeEnum.Tenant, IsSystem = true },
            new Role { Id = (int)RoleEnum.TenantMember, MasterCode = "tenant.member", FullName = "Member", Description = "Tenant member", Scope = RoleScopeEnum.Tenant, IsSystem = true },

            // Organization scope (20-29)
            new Role { Id = (int)RoleEnum.OrgAdmin, MasterCode = "org.admin", FullName = "Admin", Description = "Organization administrator", Scope = RoleScopeEnum.Organization, IsSystem = true },
            new Role { Id = (int)RoleEnum.OrgModerator, MasterCode = "org.moderator", FullName = "Moderator", Description = "Organization moderator", Scope = RoleScopeEnum.Organization, IsSystem = true },
            new Role { Id = (int)RoleEnum.OrgMember, MasterCode = "org.member", FullName = "Member", Description = "Regular organization member", Scope = RoleScopeEnum.Organization, IsSystem = true },

            // Event scope (40-49) - first-release operational roles only
            new Role { Id = (int)RoleEnum.EventOwner, MasterCode = "event.owner", FullName = "Event Owner", Description = "Owns event team authority and ownership transfer", Scope = RoleScopeEnum.Event, IsSystem = true },
            new Role { Id = (int)RoleEnum.EventManager, MasterCode = "event.manager", FullName = "Event Manager", Description = "Manages day-to-day event operations", Scope = RoleScopeEnum.Event, IsSystem = true },
            new Role { Id = (int)RoleEnum.RegistrationManager, MasterCode = "event.registration_manager", FullName = "Registration Manager", Description = "Manages registrations for one event", Scope = RoleScopeEnum.Event, IsSystem = true },
            new Role { Id = (int)RoleEnum.CheckInStaff, MasterCode = "event.check_in_staff", FullName = "Check-in Staff", Description = "Handles attendee check-in for one event", Scope = RoleScopeEnum.Event, IsSystem = true }
        };

        var existingIds = await context.Roles
            .AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync(ct);

        var existingIdSet = existingIds.ToHashSet();
        var missingRoles = expectedRoles
            .Where(x => !existingIdSet.Contains(x.Id))
            .ToList();

        if (missingRoles.Count == 0) return;

        context.Roles.AddRange(missingRoles);
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedPermissionsAsync(ExploreDbContext context, CancellationToken ct)
    {
        // Permission vocabulary: resource_kind × action pairs for all 18 resource kinds.
        // MasterCode format: "{resource_kind}:{action}" (matches Cerbos resource/action model).
        var expectedPermissions = new List<Permission>();
        var id = 1;

        // Helper to add a permission set for a resource kind
        void AddPermissions(string resourceKind, string groupName, RoleScopeEnum scope, string[] actions, bool isFiltered = false)
        {
            foreach (var action in actions)
            {
                expectedPermissions.Add(new Permission
                {
                    Id = id++,
                    ResourceKind = resourceKind,
                    Action = action,
                    MasterCode = $"{resourceKind}:{action}",
                    FullName = $"{FormatName(action)} {FormatName(resourceKind)}",
                    GroupName = groupName,
                    Scope = scope,
                    IsSystem = true,
                    IsFiltered = isFiltered,
                    IsActive = true
                });
            }
        }

        string[] crud = ["view", "create", "update", "delete"];
        string[] readOnly = ["view"];
        string[] noDelete = ["view", "create", "update"];

        // Events group
        AddPermissions("event", "Events", RoleScopeEnum.Event, crud);
        AddPermissions("event_day", "Events", RoleScopeEnum.Event, crud);
        AddPermissions("event_agenda_item", "Events", RoleScopeEnum.Event, crud);
        AddPermissions("event_session", "Events", RoleScopeEnum.Event, crud);
        AddPermissions("event_session_agenda_item", "Events", RoleScopeEnum.Event, crud);
        AddPermissions("event_registration", "Events", RoleScopeEnum.Event, crud);

        // Organizations group
        AddPermissions("organization", "Organizations", RoleScopeEnum.Organization, crud);
        AddPermissions("organization_member", "Organizations", RoleScopeEnum.Organization, crud);
        AddPermissions("organization_review", "Organizations", RoleScopeEnum.Organization, crud);

        // Content group
        AddPermissions("category", "Content", RoleScopeEnum.Tenant, crud);
        AddPermissions("tag", "Content", RoleScopeEnum.Tenant, crud);
        AddPermissions("location", "Content", RoleScopeEnum.Tenant, crud);
        AddPermissions("storage_object", "Content", RoleScopeEnum.Organization, noDelete);

        // Users group
        AddPermissions("user", "Users", RoleScopeEnum.Platform, readOnly);
        AddPermissions("tenant_user_role_grant", "Users", RoleScopeEnum.Tenant, crud);

        // Tenant management group
        AddPermissions("tenant", "Tenants", RoleScopeEnum.Platform, crud, isFiltered: true);
        AddPermissions("tenant_setting", "Settings", RoleScopeEnum.Tenant, ["view", "update"]);

        // Instance settings (platform-only, filtered from non-super-admins)
        AddPermissions("instance_setting", "Settings", RoleScopeEnum.Platform, ["view", "update"], isFiltered: true);

        // Federation group
        AddPermissions("indexed_did", "Federation", RoleScopeEnum.Platform, noDelete);
        AddPermissions("atproto_record", "Federation", RoleScopeEnum.Platform, noDelete);

        // Event operational roles group (event-scoped v1 vocabulary)
        AddPermissions("event", "Event Operations", RoleScopeEnum.Event, ["manage-team", "manage-owner", "transfer-ownership", "manage-finance"]);
        AddPermissions("event_registration", "Event Operations", RoleScopeEnum.Event, ["manage"]);
        AddPermissions("event_check_in", "Event Operations", RoleScopeEnum.Event, ["view", "manage"]);

        var existingCodes = await context.Permissions
            .AsNoTracking()
            .Select(x => x.MasterCode)
            .ToListAsync(ct);

        var existingCodeSet = existingCodes.ToHashSet();
        var missingPermissions = expectedPermissions
            .Where(x => !existingCodeSet.Contains(x.MasterCode))
            .ToList();

        if (missingPermissions.Count > 0)
        {
            context.Permissions.AddRange(missingPermissions);
            await context.SaveChangesAsync(ct);
        }

        await EnsureEventPermissionScopesAsync(context, ct);
    }

    private static async Task EnsureEventPermissionScopesAsync(ExploreDbContext context, CancellationToken ct)
    {
        var eventPermissionCodes = new HashSet<string>(StringComparer.Ordinal)
        {
            "event:view",
            PermissionCodes.EventCreate,
            PermissionCodes.EventUpdate,
            PermissionCodes.EventDelete,
            PermissionCodes.EventPublish,
            "event_day:view",
            PermissionCodes.EventDayCreate,
            PermissionCodes.EventDayUpdate,
            PermissionCodes.EventDayDelete,
            "event_agenda_item:view",
            PermissionCodes.EventAgendaItemCreate,
            PermissionCodes.EventAgendaItemUpdate,
            PermissionCodes.EventAgendaItemDelete,
            "event_session:view",
            PermissionCodes.EventSessionCreate,
            PermissionCodes.EventSessionUpdate,
            PermissionCodes.EventSessionDelete,
            "event_session_agenda_item:view",
            "event_session_agenda_item:create",
            "event_session_agenda_item:update",
            "event_session_agenda_item:delete",
            PermissionCodes.EventRegistrationView,
            "event_registration:create",
            "event_registration:update",
            "event_registration:delete",
            PermissionCodes.EventManageTeam,
            PermissionCodes.EventManageOwner,
            PermissionCodes.EventTransferOwnership,
            PermissionCodes.EventManageFinance,
            PermissionCodes.EventRegistrationManage,
            PermissionCodes.EventCheckInView,
            PermissionCodes.EventCheckInManage
        };

        var eventPermissions = await context.Permissions
            .Where(p => eventPermissionCodes.Contains(p.MasterCode) && p.RoleScopeId != (int)RoleScopeEnum.Event)
            .ToListAsync(ct);

        if (eventPermissions.Count == 0)
        {
            return;
        }

        foreach (var permission in eventPermissions)
        {
            permission.Scope = RoleScopeEnum.Event;
        }

        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedEventRolePermissionsAsync(ExploreDbContext context, CancellationToken ct)
    {
        var rolePermissionCodes = new Dictionary<RoleEnum, string[]>
        {
            [RoleEnum.EventOwner] =
            [
                "event:view",
                PermissionCodes.EventCreate,
                PermissionCodes.EventUpdate,
                PermissionCodes.EventDelete,
                PermissionCodes.EventPublish,
                PermissionCodes.EventManageTeam,
                PermissionCodes.EventManageOwner,
                PermissionCodes.EventTransferOwnership,
                PermissionCodes.EventManageFinance,
                "event_day:view",
                PermissionCodes.EventDayCreate,
                PermissionCodes.EventDayUpdate,
                PermissionCodes.EventDayDelete,
                "event_agenda_item:view",
                PermissionCodes.EventAgendaItemCreate,
                PermissionCodes.EventAgendaItemUpdate,
                PermissionCodes.EventAgendaItemDelete,
                "event_session:view",
                PermissionCodes.EventSessionCreate,
                PermissionCodes.EventSessionUpdate,
                PermissionCodes.EventSessionDelete,
                "event_session_agenda_item:view",
                "event_session_agenda_item:create",
                "event_session_agenda_item:update",
                "event_session_agenda_item:delete",
                PermissionCodes.EventRegistrationView,
                "event_registration:create",
                "event_registration:update",
                "event_registration:delete",
                PermissionCodes.EventRegistrationManage,
                PermissionCodes.EventCheckInView,
                PermissionCodes.EventCheckInManage
            ],
            [RoleEnum.EventManager] =
            [
                "event:view",
                PermissionCodes.EventUpdate,
                PermissionCodes.EventPublish,
                PermissionCodes.EventManageTeam,
                "event_day:view",
                PermissionCodes.EventDayCreate,
                PermissionCodes.EventDayUpdate,
                PermissionCodes.EventDayDelete,
                "event_agenda_item:view",
                PermissionCodes.EventAgendaItemCreate,
                PermissionCodes.EventAgendaItemUpdate,
                PermissionCodes.EventAgendaItemDelete,
                "event_session:view",
                PermissionCodes.EventSessionCreate,
                PermissionCodes.EventSessionUpdate,
                PermissionCodes.EventSessionDelete,
                "event_session_agenda_item:view",
                "event_session_agenda_item:create",
                "event_session_agenda_item:update",
                "event_session_agenda_item:delete",
                PermissionCodes.EventRegistrationView,
                PermissionCodes.EventRegistrationManage,
                PermissionCodes.EventCheckInView,
                PermissionCodes.EventCheckInManage
            ],
            [RoleEnum.RegistrationManager] =
            [
                "event:view",
                PermissionCodes.EventRegistrationView,
                PermissionCodes.EventRegistrationManage
            ],
            [RoleEnum.CheckInStaff] =
            [
                "event:view",
                PermissionCodes.EventRegistrationView,
                PermissionCodes.EventCheckInView,
                PermissionCodes.EventCheckInManage
            ]
        };

        var requiredPermissionCodes = rolePermissionCodes.Values
            .SelectMany(codes => codes)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var permissionIdsByCode = await context.Permissions
            .AsNoTracking()
            .Where(p => requiredPermissionCodes.Contains(p.MasterCode))
            .ToDictionaryAsync(p => p.MasterCode, p => p.Id, ct);

        var roleIds = rolePermissionCodes.Keys
            .Select(role => (int)role)
            .ToArray();

        var existingPairs = await context.RolePermissions
            .AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync(ct);

        var existingPairSet = existingPairs
            .Select(pair => (pair.RoleId, pair.PermissionId))
            .ToHashSet();

        var grantedAt = DateTime.UtcNow;
        var missingRolePermissions = new List<RolePermission>();

        foreach (var (role, permissionCodes) in rolePermissionCodes)
        {
            foreach (var permissionCode in permissionCodes)
            {
                if (!permissionIdsByCode.TryGetValue(permissionCode, out var permissionId) ||
                    existingPairSet.Contains(((int)role, permissionId)))
                {
                    continue;
                }

                missingRolePermissions.Add(new RolePermission
                {
                    RoleId = (int)role,
                    PermissionId = permissionId,
                    GrantedAt = grantedAt,
                    Role = null!,
                    Permission = null!
                });
            }
        }

        if (missingRolePermissions.Count == 0)
        {
            return;
        }

        context.RolePermissions.AddRange(missingRolePermissions);
        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Formats a snake_case identifier to Title Case for display.
    /// </summary>
    private static string FormatName(string identifier)
    {
        return string.Join(' ', identifier.Split('_')
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
    }

    private static async Task SeedNotificationTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<NotificationType>().AnyAsync(ct)) return;

        context.Set<NotificationType>().AddRange(
            new NotificationType { Id = (int)NotificationTypeEnum.RegistrationConfirmed, MasterCode = "REGISTRATION_CONFIRMED", FullName = "Registration Confirmed", Description = "RSVP or registration was confirmed" },
            new NotificationType { Id = (int)NotificationTypeEnum.ApprovalGranted, MasterCode = "APPROVAL_GRANTED", FullName = "Approval Granted", Description = "An approval request was granted" },
            new NotificationType { Id = (int)NotificationTypeEnum.ApprovalRejected, MasterCode = "APPROVAL_REJECTED", FullName = "Approval Rejected", Description = "An approval request was rejected" },
            new NotificationType { Id = (int)NotificationTypeEnum.WaitlistPromoted, MasterCode = "WAITLIST_PROMOTED", FullName = "Waitlist Promoted", Description = "Promoted from waitlist to confirmed" },
            new NotificationType { Id = (int)NotificationTypeEnum.EventCreated, MasterCode = "EVENT_CREATED", FullName = "Event Created", Description = "A new event was created" },
            new NotificationType { Id = (int)NotificationTypeEnum.EventUpdated, MasterCode = "EVENT_UPDATED", FullName = "Event Updated", Description = "An event was updated" },
            new NotificationType { Id = (int)NotificationTypeEnum.EventCancelled, MasterCode = "EVENT_CANCELLED", FullName = "Event Cancelled", Description = "An event was cancelled" },
            new NotificationType { Id = (int)NotificationTypeEnum.MemberInvited, MasterCode = "MEMBER_INVITED", FullName = "Member Invited", Description = "Invited to join an organization or group" },
            new NotificationType { Id = (int)NotificationTypeEnum.MemberRemoved, MasterCode = "MEMBER_REMOVED", FullName = "Member Removed", Description = "Removed from an organization or group" },
            new NotificationType { Id = (int)NotificationTypeEnum.General, MasterCode = "GENERAL", FullName = "General", Description = "General purpose notification" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedNotificationEntityTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<NotificationEntityType>().AnyAsync(ct)) return;

        context.Set<NotificationEntityType>().AddRange(
            new NotificationEntityType { Id = (int)NotificationEntityTypeEnum.Event, MasterCode = "EVENT", FullName = "Event", Description = "Links to an event" },
            new NotificationEntityType { Id = (int)NotificationEntityTypeEnum.Organization, MasterCode = "ORGANIZATION", FullName = "Organization", Description = "Links to an organization" },
            new NotificationEntityType { Id = (int)NotificationEntityTypeEnum.Group, MasterCode = "GROUP", FullName = "Group", Description = "Links to a group" },
            new NotificationEntityType { Id = (int)NotificationEntityTypeEnum.EventRegistration, MasterCode = "EVENT_REGISTRATION", FullName = "Event Registration", Description = "Links to an event registration" },
            new NotificationEntityType { Id = (int)NotificationEntityTypeEnum.EventSession, MasterCode = "EVENT_SESSION", FullName = "Event Session", Description = "Links to an event session" },
            new NotificationEntityType { Id = (int)NotificationEntityTypeEnum.User, MasterCode = "USER", FullName = "User", Description = "Links to a user" });
        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Seeds default instance-level footer link groups (TenantId = null) with standard navigation links.
    /// Only runs if no instance-level footer link groups exist yet.
    /// </summary>
    private static async Task SeedDefaultFooterLinkGroupsAsync(ExploreDbContext context, CancellationToken ct)
    {
        // Only seed if no instance-level (TenantId = null) footer link groups exist
        if (await context.Set<TenantFooterLinkGroup>().AnyAsync(g => g.TenantId == null, ct)) return;

        var now = DateTime.UtcNow;

        // Group 1: Quick Links
        var quickLinksGroup = new TenantFooterLinkGroup
        {
            Id = Guid.Parse("019573a0-0001-7000-8000-000000000001"),
            TenantId = null,
            Title = "Quick Links",
            Order = 0,
            IsActive = true,
            CreatedAt = now,
        };

        // Group 2: Legal
        var legalGroup = new TenantFooterLinkGroup
        {
            Id = Guid.Parse("019573a0-0002-7000-8000-000000000001"),
            TenantId = null,
            Title = "Legal",
            Order = 1,
            IsActive = true,
            CreatedAt = now,
        };

        context.Set<TenantFooterLinkGroup>().AddRange(quickLinksGroup, legalGroup);
        await context.SaveChangesAsync(ct);

        // Quick Links
        context.Set<TenantFooterLink>().AddRange(
            new TenantFooterLink
            {
                Id = Guid.Parse("019573a0-0003-7000-8000-000000000001"),
                FooterLinkGroupId = quickLinksGroup.Id,
                Label = "About Us",
                Url = "/about",
                OpenInNewTab = false,
                Order = 0,
                IsActive = true,
                CreatedAt = now,
            },
            new TenantFooterLink
            {
                Id = Guid.Parse("019573a0-0004-7000-8000-000000000001"),
                FooterLinkGroupId = quickLinksGroup.Id,
                Label = "Events",
                Url = "/events",
                OpenInNewTab = false,
                Order = 1,
                IsActive = true,
                CreatedAt = now,
            },
            new TenantFooterLink
            {
                Id = Guid.Parse("019573a0-0005-7000-8000-000000000001"),
                FooterLinkGroupId = quickLinksGroup.Id,
                Label = "Contact",
                Url = "/contact",
                OpenInNewTab = false,
                Order = 2,
                IsActive = true,
                CreatedAt = now,
            });

        // Legal
        context.Set<TenantFooterLink>().AddRange(
            new TenantFooterLink
            {
                Id = Guid.Parse("019573a0-0006-7000-8000-000000000001"),
                FooterLinkGroupId = legalGroup.Id,
                Label = "Terms of Service",
                Url = "/terms",
                OpenInNewTab = false,
                Order = 0,
                IsActive = true,
                CreatedAt = now,
            },
            new TenantFooterLink
            {
                Id = Guid.Parse("019573a0-0007-7000-8000-000000000001"),
                FooterLinkGroupId = legalGroup.Id,
                Label = "Privacy Policy",
                Url = "/privacy",
                OpenInNewTab = false,
                Order = 1,
                IsActive = true,
                CreatedAt = now,
            });

        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedExternalApiKeyStatusesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<ExternalApiKeyStatus>().AnyAsync(ct)) return;

        context.Set<ExternalApiKeyStatus>().AddRange(
            new ExternalApiKeyStatus { Id = (int)ExternalApiKeyStatusEnum.Active, MasterCode = "ACTIVE", FullName = "Active", Description = "Key is active and can authenticate requests", IsUsable = true },
            new ExternalApiKeyStatus { Id = (int)ExternalApiKeyStatusEnum.Revoked, MasterCode = "REVOKED", FullName = "Revoked", Description = "Key has been permanently revoked by owner or admin", IsUsable = false },
            new ExternalApiKeyStatus { Id = (int)ExternalApiKeyStatusEnum.Expired, MasterCode = "EXPIRED", FullName = "Expired", Description = "Key has passed its expiration date", IsUsable = false },
            new ExternalApiKeyStatus { Id = (int)ExternalApiKeyStatusEnum.Suspended, MasterCode = "SUSPENDED", FullName = "Suspended", Description = "Key is temporarily suspended due to credit exhaustion or policy violation", IsUsable = false },
            new ExternalApiKeyStatus { Id = (int)ExternalApiKeyStatusEnum.PendingRotation, MasterCode = "PENDING_ROTATION", FullName = "Pending Rotation", Description = "Key is in rotation overlap window; still usable until new key is confirmed", IsUsable = true });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedExternalApiKeyCreditPeriodsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<ExternalApiKeyCreditPeriod>().AnyAsync(ct)) return;

        context.Set<ExternalApiKeyCreditPeriod>().AddRange(
            new ExternalApiKeyCreditPeriod { Id = (int)ExternalApiKeyCreditPeriodEnum.None, MasterCode = "NONE", FullName = "None", Description = "No credit tracking; unlimited usage" },
            new ExternalApiKeyCreditPeriod { Id = (int)ExternalApiKeyCreditPeriodEnum.Daily, MasterCode = "DAILY", FullName = "Daily", Description = "Credit quota resets every day" },
            new ExternalApiKeyCreditPeriod { Id = (int)ExternalApiKeyCreditPeriodEnum.Weekly, MasterCode = "WEEKLY", FullName = "Weekly", Description = "Credit quota resets every week" },
            new ExternalApiKeyCreditPeriod { Id = (int)ExternalApiKeyCreditPeriodEnum.Monthly, MasterCode = "MONTHLY", FullName = "Monthly", Description = "Credit quota resets every month" },
            new ExternalApiKeyCreditPeriod { Id = (int)ExternalApiKeyCreditPeriodEnum.Yearly, MasterCode = "YEARLY", FullName = "Yearly", Description = "Credit quota resets every year" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedNotificationReasonsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<NotificationReason>().AnyAsync(ct)) return;

        context.Set<NotificationReason>().AddRange(
            new NotificationReason { Id = (int)NotificationReasonEnum.Direct, MasterCode = "DIRECT", FullName = "Direct", Description = "Notification sent directly to the user" },
            new NotificationReason { Id = (int)NotificationReasonEnum.Mention, MasterCode = "MENTION", FullName = "Mention", Description = "User was mentioned" },
            new NotificationReason { Id = (int)NotificationReasonEnum.Assignment, MasterCode = "ASSIGNMENT", FullName = "Assignment", Description = "User was assigned a task or role" },
            new NotificationReason { Id = (int)NotificationReasonEnum.Subscription, MasterCode = "SUBSCRIPTION", FullName = "Subscription", Description = "User is subscribed to the source" },
            new NotificationReason { Id = (int)NotificationReasonEnum.Membership, MasterCode = "MEMBERSHIP", FullName = "Membership", Description = "User is a member of the related entity" },
            new NotificationReason { Id = (int)NotificationReasonEnum.System, MasterCode = "SYSTEM", FullName = "System", Description = "System-generated notification" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedUiThemePresetsAsync(ExploreDbContext context, CancellationToken ct)
    {
        var currentSeedVersion = 6;
        var existingPresets = await context.UiThemePresets
            .Where(p => p.IsSystem && p.TenantId == null)
            .ToListAsync(ct);

        var alreadySeeded = existingPresets.Any(p => p.SeedVersion >= currentSeedVersion);
        if (alreadySeeded) return;

        var enterpriseBlue = new UiThemePreset
        {
            Id = Guid.Parse("a1b2c3d4-1111-1111-1111-111111111111"),
            TenantId = null,
            ThemeKey = "enterprise-blue",
            DisplayName = "Enterprise Blue",
            Description = "Default professional theme with a blue accent palette.",
            IsSystem = true,
            IsEditable = false,
            IsActive = true,
            SeedVersion = currentSeedVersion,
            LightPalette = new Domain.ValueObjects.UiThemePalette
            {
                Primary = "#18181B",
                PrimaryContrastText = "#FFFFFF",
                Secondary = "#52525B",
                SecondaryContrastText = "#FFFFFF",
                Background = "#F5F5F7",
                Surface = "#FFFFFF",
                AppbarBackground = "#FFFFFF",
                AppbarText = "#18181B",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#18181B",
                DrawerIcon = "#52525B",
                TextPrimary = "#18181B",
                TextSecondary = "#404040",
                Info = "#52525B",
                Success = "#16A34A",
                Warning = "#D97706",
                Error = "#DC2626",
                LinesDefault = "#A1A1AA",
                Divider = "#E4E4E7"
            },
            DarkPalette = new Domain.ValueObjects.UiThemePalette
            {
                Primary = "#FAFAFA",
                PrimaryContrastText = "#1A1A1A",
                Secondary = "#A1A1AA",
                SecondaryContrastText = "#1A1A1A",
                Background = "#1A1A1A",
                Surface = "#242424",
                AppbarBackground = "rgba(26,26,26,0.92)",
                AppbarText = "#FAFAFA",
                DrawerBackground = "#1A1A1A",
                DrawerText = "#FAFAFA",
                DrawerIcon = "#A1A1AA",
                TextPrimary = "#FAFAFA",
                TextSecondary = "#A1A1AA",
                Info = "#A1A1AA",
                Success = "#34D399",
                Warning = "#FBBF24",
                Error = "#F87171",
                LinesDefault = "#3F3F46",
                Divider = "#2E2E2E"
            }
        };

        var emeraldGreen = new UiThemePreset
        {
            Id = Guid.Parse("a1b2c3d4-2222-2222-2222-222222222222"),
            TenantId = null,
            ThemeKey = "emerald-green",
            DisplayName = "Emerald Green",
            Description = "Fresh and natural theme with green accents, ideal for Islamic event branding.",
            IsSystem = true,
            IsEditable = false,
            IsActive = true,
            SeedVersion = currentSeedVersion,
            LightPalette = new Domain.ValueObjects.UiThemePalette
            {
                Primary = "#16A34A",
                PrimaryContrastText = "#FFFFFF",
                Secondary = "#52525B",
                SecondaryContrastText = "#FFFFFF",
                Background = "#F5F5F7",
                Surface = "#FFFFFF",
                AppbarBackground = "#FFFFFF",
                AppbarText = "#18181B",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#18181B",
                DrawerIcon = "#52525B",
                TextPrimary = "#18181B",
                TextSecondary = "#52525B",
                Info = "#2563EB",
                Success = "#16A34A",
                Warning = "#D97706",
                Error = "#DC2626",
                LinesDefault = "#D4D4D8",
                Divider = "#E4E4E7"
            },
            DarkPalette = new Domain.ValueObjects.UiThemePalette
            {
                Primary = "#22C55E",
                PrimaryContrastText = "#18181B",
                Secondary = "#E4E4E7",
                SecondaryContrastText = "#18181B",
                Background = "#121212",
                Surface = "#1E1E1E",
                AppbarBackground = "rgba(18,18,18,0.92)",
                AppbarText = "#FAFAFA",
                DrawerBackground = "#121212",
                DrawerText = "#FAFAFA",
                DrawerIcon = "#A1A1AA",
                TextPrimary = "#FAFAFA",
                TextSecondary = "#A1A1AA",
                Info = "#60A5FA",
                Success = "#22C55E",
                Warning = "#FBBF24",
                Error = "#F87171",
                LinesDefault = "#3F3F46",
                Divider = "#27272A"
            }
        };

        var abyssalDark = new UiThemePreset
        {
            Id = Guid.Parse("a1b2c3d4-3333-3333-3333-333333333333"),
            TenantId = null,
            ThemeKey = "abyssal-dark",
            DisplayName = "Abyssal Dark",
            Description = "Dark-first theme with deep charcoal tones, still offering a light palette.",
            IsSystem = true,
            IsEditable = false,
            IsActive = true,
            SeedVersion = currentSeedVersion,
            LightPalette = new Domain.ValueObjects.UiThemePalette
            {
                Primary = "#0F62FE",
                PrimaryContrastText = "#FFFFFF",
                Secondary = "#52525B",
                SecondaryContrastText = "#FFFFFF",
                Background = "#F5F5F7",
                Surface = "#FFFFFF",
                AppbarBackground = "#FFFFFF",
                AppbarText = "#18181B",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#18181B",
                DrawerIcon = "#71717A",
                TextPrimary = "#18181B",
                TextSecondary = "#71717A",
                Info = "#2563EB",
                Success = "#16A34A",
                Warning = "#D97706",
                Error = "#DC2626",
                LinesDefault = "#D4D4D8",
                Divider = "#E4E4E7"
            },
            DarkPalette = new Domain.ValueObjects.UiThemePalette
            {
                Primary = "#60A5FA",
                PrimaryContrastText = "#18181B",
                Secondary = "#A1A1AA",
                SecondaryContrastText = "#18181B",
                Background = "#09090B",
                Surface = "#18181B",
                AppbarBackground = "rgba(9,9,11,0.95)",
                AppbarText = "#FAFAFA",
                DrawerBackground = "#09090B",
                DrawerText = "#FAFAFA",
                DrawerIcon = "#71717A",
                TextPrimary = "#FAFAFA",
                TextSecondary = "#A1A1AA",
                Info = "#60A5FA",
                Success = "#34D399",
                Warning = "#FBBF24",
                Error = "#F87171",
                LinesDefault = "#27272A",
                Divider = "#27272A"
            }
        };

        var pureWhite = new UiThemePreset
        {
            Id = Guid.Parse("a1b2c3d4-4444-4444-4444-444444444444"),
            TenantId = null,
            ThemeKey = "pure-white",
            DisplayName = "Pure White",
            Description = "Minimal clean theme with subtle neutral boundaries for maximum clarity.",
            IsSystem = true,
            IsEditable = false,
            IsActive = true,
            SeedVersion = currentSeedVersion,
            LightPalette = new Domain.ValueObjects.UiThemePalette
            {
                Primary = "#3B82F6",
                PrimaryContrastText = "#FFFFFF",
                Secondary = "#71717A",
                SecondaryContrastText = "#FFFFFF",
                Background = "#FFFFFF",
                Surface = "#FAFAFA",
                AppbarBackground = "#FFFFFF",
                AppbarText = "#18181B",
                DrawerBackground = "#FAFAFA",
                DrawerText = "#18181B",
                DrawerIcon = "#71717A",
                TextPrimary = "#18181B",
                TextSecondary = "#71717A",
                Info = "#3B82F6",
                Success = "#10B981",
                Warning = "#F59E0B",
                Error = "#EF4444",
                LinesDefault = "#E4E4E7",
                Divider = "#F4F4F5"
            },
            DarkPalette = new Domain.ValueObjects.UiThemePalette
            {
                Primary = "#60A5FA",
                PrimaryContrastText = "#18181B",
                Secondary = "#A1A1AA",
                SecondaryContrastText = "#18181B",
                Background = "#121212",
                Surface = "#1E1E1E",
                AppbarBackground = "#121212",
                AppbarText = "#FAFAFA",
                DrawerBackground = "#121212",
                DrawerText = "#FAFAFA",
                DrawerIcon = "#71717A",
                TextPrimary = "#FAFAFA",
                TextSecondary = "#A1A1AA",
                Info = "#60A5FA",
                Success = "#34D399",
                Warning = "#FBBF24",
                Error = "#F87171",
                LinesDefault = "#3F3F46",
                Divider = "#3F3F46"
            }
        };

        var lightHighContrast = new UiThemePreset
        {
            Id = Guid.Parse("a1b2c3d4-5555-5555-5555-555555555555"),
            TenantId = null,
            ThemeKey = "light-hc",
            DisplayName = "Light High Contrast",
            Description = "WCAG AAA-compliant light theme with maximum text contrast for accessibility.",
            IsSystem = true,
            IsEditable = false,
            IsActive = true,
            SeedVersion = currentSeedVersion,
            LightPalette = new Domain.ValueObjects.UiThemePalette
            {
                Primary = "#0050D8",
                PrimaryContrastText = "#FFFFFF",
                Secondary = "#1E293B",
                SecondaryContrastText = "#FFFFFF",
                Background = "#FFFFFF",
                Surface = "#FFFFFF",
                AppbarBackground = "#FFFFFF",
                AppbarText = "#000000",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#000000",
                DrawerIcon = "#000000",
                TextPrimary = "#000000",
                TextSecondary = "#1E293B",
                Info = "#0050D8",
                Success = "#006600",
                Warning = "#B45309",
                Error = "#B91C1C",
                LinesDefault = "#000000",
                Divider = "#000000"
            },
            DarkPalette = new Domain.ValueObjects.UiThemePalette
            {
                Primary = "#0050D8",
                PrimaryContrastText = "#FFFFFF",
                Secondary = "#1E293B",
                SecondaryContrastText = "#FFFFFF",
                Background = "#F8FAFC",
                Surface = "#FFFFFF",
                AppbarBackground = "#FFFFFF",
                AppbarText = "#000000",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#000000",
                DrawerIcon = "#000000",
                TextPrimary = "#000000",
                TextSecondary = "#1E293B",
                Info = "#0050D8",
                Success = "#006600",
                Warning = "#B45309",
                Error = "#B91C1C",
                LinesDefault = "#000000",
                Divider = "#000000"
            }
        };

        var darkHighContrast = new UiThemePreset
        {
            Id = Guid.Parse("a1b2c3d4-6666-6666-6666-666666666666"),
            TenantId = null,
            ThemeKey = "dark-hc",
            DisplayName = "Dark High Contrast",
            Description = "WCAG AAA-compliant dark theme with pure white text on black backgrounds for maximum readability.",
            IsSystem = true,
            IsEditable = false,
            IsActive = true,
            SeedVersion = currentSeedVersion,
            LightPalette = new Domain.ValueObjects.UiThemePalette
            {
                Primary = "#93C5FD",
                PrimaryContrastText = "#000000",
                Secondary = "#F8FAFC",
                SecondaryContrastText = "#000000",
                Background = "#FFFFFF",
                Surface = "#F9FAFB",
                AppbarBackground = "#FFFFFF",
                AppbarText = "#000000",
                DrawerBackground = "#F9FAFB",
                DrawerText = "#000000",
                DrawerIcon = "#000000",
                TextPrimary = "#000000",
                TextSecondary = "#1E293B",
                Info = "#2563EB",
                Success = "#16A34A",
                Warning = "#D97706",
                Error = "#DC2626",
                LinesDefault = "#1E293B",
                Divider = "#E2E8F0"
            },
            DarkPalette = new Domain.ValueObjects.UiThemePalette
            {
                Primary = "#93C5FD",
                PrimaryContrastText = "#000000",
                Secondary = "#F8FAFC",
                SecondaryContrastText = "#000000",
                Background = "#000000",
                Surface = "#0A0A0A",
                AppbarBackground = "#000000",
                AppbarText = "#FFFFFF",
                DrawerBackground = "#000000",
                DrawerText = "#FFFFFF",
                DrawerIcon = "#FFFFFF",
                TextPrimary = "#FFFFFF",
                TextSecondary = "#E2E8F0",
                Info = "#93C5FD",
                Success = "#6EE7B7",
                Warning = "#FCD34D",
                Error = "#FCA5A5",
                LinesDefault = "#FFFFFF",
                Divider = "#FFFFFF"
            }
        };

        var white = new UiThemePreset
        {
            Id = Guid.Parse("a1b2c3d4-7777-7777-7777-777777777777"),
            TenantId = null,
            ThemeKey = "classic-white",
            DisplayName = "White",
            Description = "Clean, bright theme with pure white surfaces and crisp blue accents for a professional look.",
            IsSystem = true,
            IsEditable = false,
            IsActive = true,
            SeedVersion = currentSeedVersion,
            LightPalette = new Domain.ValueObjects.UiThemePalette
            {
                Primary = "#2563EB",
                PrimaryContrastText = "#FFFFFF",
                Secondary = "#71717A",
                SecondaryContrastText = "#FFFFFF",
                Background = "#FFFFFF",
                Surface = "#FFFFFF",
                AppbarBackground = "#FFFFFF",
                AppbarText = "#18181B",
                DrawerBackground = "#FAFAFA",
                DrawerText = "#18181B",
                DrawerIcon = "#71717A",
                TextPrimary = "#18181B",
                TextSecondary = "#52525B",
                Info = "#2563EB",
                Success = "#16A34A",
                Warning = "#D97706",
                Error = "#DC2626",
                LinesDefault = "#D4D4D8",
                Divider = "#E4E4E7"
            },
            DarkPalette = new Domain.ValueObjects.UiThemePalette
            {
                Primary = "#60A5FA",
                PrimaryContrastText = "#18181B",
                Secondary = "#A1A1AA",
                SecondaryContrastText = "#18181B",
                Background = "#121212",
                Surface = "#1E1E1E",
                AppbarBackground = "rgba(18,18,18,0.92)",
                AppbarText = "#FAFAFA",
                DrawerBackground = "#121212",
                DrawerText = "#FAFAFA",
                DrawerIcon = "#A1A1AA",
                TextPrimary = "#FAFAFA",
                TextSecondary = "#A1A1AA",
                Info = "#60A5FA",
                Success = "#34D399",
                Warning = "#FBBF24",
                Error = "#F87171",
                LinesDefault = "#3F3F46",
                Divider = "#27272A"
            }
        };

        var dark = new UiThemePreset
        {
            Id = Guid.Parse("a1b2c3d4-8888-8888-8888-888888888888"),
            TenantId = null,
            ThemeKey = "classic-dark",
            DisplayName = "Dark",
            Description = "Refined dark theme with deep charcoal surfaces and vibrant accents for comfortable extended use.",
            IsSystem = true,
            IsEditable = false,
            IsActive = true,
            SeedVersion = currentSeedVersion,
            LightPalette = new Domain.ValueObjects.UiThemePalette
            {
                Primary = "#2563EB",
                PrimaryContrastText = "#FFFFFF",
                Secondary = "#64748B",
                SecondaryContrastText = "#FFFFFF",
                Background = "#F8FAFC",
                Surface = "#FFFFFF",
                AppbarBackground = "#FFFFFF",
                AppbarText = "#0F172A",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#0F172A",
                DrawerIcon = "#64748B",
                TextPrimary = "#0F172A",
                TextSecondary = "#475569",
                Info = "#2563EB",
                Success = "#16A34A",
                Warning = "#D97706",
                Error = "#DC2626",
                LinesDefault = "#E2E8F0",
                Divider = "#E2E8F0"
            },
            DarkPalette = new Domain.ValueObjects.UiThemePalette
            {
                Primary = "#818CF8",
                PrimaryContrastText = "#0F172A",
                Secondary = "#A1A1AA",
                SecondaryContrastText = "#0F172A",
                Background = "#09090B",
                Surface = "#18181B",
                AppbarBackground = "rgba(9,9,11,0.92)",
                AppbarText = "#FAFAFA",
                DrawerBackground = "#09090B",
                DrawerText = "#FAFAFA",
                DrawerIcon = "#A1A1AA",
                TextPrimary = "#FAFAFA",
                TextSecondary = "#A1A1AA",
                Info = "#60A5FA",
                Success = "#4ADE80",
                Warning = "#FACC15",
                Error = "#F87171",
                LinesDefault = "#27272A",
                Divider = "#27272A"
            }
        };

        var presets = new[] { enterpriseBlue, emeraldGreen, abyssalDark, pureWhite, lightHighContrast, darkHighContrast, white, dark };

        foreach (var preset in presets)
        {
            var existing = existingPresets.FirstOrDefault(p => p.ThemeKey == preset.ThemeKey);
            if (existing is not null)
            {
                existing.DisplayName = preset.DisplayName;
                existing.Description = preset.Description;
                existing.LightPalette = preset.LightPalette;
                existing.DarkPalette = preset.DarkPalette;
                existing.SeedVersion = currentSeedVersion;
                existing.IsActive = true;
                existing.UpdatedAt = DateTime.UtcNow;
                context.UiThemePresets.Update(existing);
            }
            else
            {
                preset.CreatedAt = DateTime.UtcNow;
                context.UiThemePresets.Add(preset);
            }
        }

        await context.SaveChangesAsync(ct);
    }
}
