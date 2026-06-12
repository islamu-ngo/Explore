// ABOUTME: Maps untrusted AI CreateEventDraft proposals into safe draft event DTOs.
// ABOUTME: Rejects unknown, privileged, out-of-scope, and invalid fields before confirmation can create events.

using System.Text.Json;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.AiAssistant.Prompting;
using Explore.Application.Features.AiAssistant.Tools;
using Explore.Domain.Ai;

namespace Explore.Application.Features.AiAssistant.Actions;

public sealed class CreateEventDraftAiActionMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public CreateEventDraftAiActionMappingResult Map(
        AiParsedProposedAction action,
        CreateEventDraftAiActionMappingContext? context = null)
    {
        if (action.Kind != AiProposedActionKind.CreateEventDraft)
        {
            return CreateEventDraftAiActionMappingResult.Failure(
                "unsupported_action_kind",
                "AI proposed action kind is not supported for event draft mapping.");
        }

        return Map(action.PayloadJson, context);
    }

    public CreateEventDraftAiActionMappingResult Map(
        string payloadJson,
        CreateEventDraftAiActionMappingContext? context = null)
    {
        context ??= CreateEventDraftAiActionMappingContext.Empty;

        CreateEventDraftAiActionPayload? payload;
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return CreateEventDraftAiActionMappingResult.Failure(
                    "invalid_payload_json",
                    "AI event draft payload must be a JSON object.");
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!CreateEventDraftAiToolDefinition.AllowedPayloadFields.Contains(property.Name))
                {
                    return CreateEventDraftAiActionMappingResult.Failure(
                        "unsupported_payload_field",
                        "AI event draft payload contains a field that is not allowed.");
                }
            }

            payload = document.RootElement.Deserialize<CreateEventDraftAiActionPayload>(JsonOptions);
        }
        catch (JsonException)
        {
            return CreateEventDraftAiActionMappingResult.Failure(
                "invalid_payload_json",
                "AI event draft payload must be valid JSON.");
        }

        if (payload is null)
        {
            return CreateEventDraftAiActionMappingResult.Failure(
                "invalid_payload_json",
                "AI event draft payload could not be read.");
        }

        var title = Normalize(payload.Title);
        if (string.IsNullOrWhiteSpace(title))
        {
            return CreateEventDraftAiActionMappingResult.Failure(
                "missing_title",
                "AI event draft payload must include a title.");
        }

        if (!ValidateLength(title, 200, "title", out var lengthFailure)
            || !ValidateLength(payload.Subtitle, 200, "subtitle", out lengthFailure)
            || !ValidateLength(payload.Description, 150, "description", out lengthFailure)
            || !ValidateLength(payload.Content, 5000, "content", out lengthFailure)
            || !ValidateLength(payload.Slug, 500, "slug", out lengthFailure)
            || !ValidateLength(payload.CurrencyCode, 3, "currencyCode", out lengthFailure)
            || !ValidateLength(payload.ExternalRegistrationUrl, 500, "externalRegistrationUrl", out lengthFailure)
            || !ValidateLength(payload.Timezone, 100, "timezone", out lengthFailure)
            || !ValidateLength(payload.EventTimeZoneId, 100, "eventTimeZoneId", out lengthFailure)
            || !ValidateLength(payload.EventUrl, 500, "eventUrl", out lengthFailure))
        {
            return lengthFailure!;
        }

        if (payload.Price < 0)
        {
            return CreateEventDraftAiActionMappingResult.Failure(
                "invalid_numeric_value",
                "AI event draft price cannot be negative.");
        }

        var hasForcedOwnerScope = context.HasForcedOwnerScope;
        if (!hasForcedOwnerScope && payload.OrganizationId.HasValue && payload.GroupId.HasValue)
        {
            return CreateEventDraftAiActionMappingResult.Failure(
                "conflicting_owner_scope",
                "AI event draft payload cannot target both an organization and a group.");
        }

        if (!hasForcedOwnerScope
            && payload.OrganizationId.HasValue
            && !context.AllowedOrganizationIds.Contains(payload.OrganizationId.Value))
        {
            return CreateEventDraftAiActionMappingResult.Failure(
                "invalid_organization_scope",
                "AI event draft organization is not allowed for this mapping context.");
        }

        if (!hasForcedOwnerScope
            && payload.GroupId.HasValue
            && !context.AllowedGroupIds.Contains(payload.GroupId.Value))
        {
            return CreateEventDraftAiActionMappingResult.Failure(
                "invalid_group_scope",
                "AI event draft group is not allowed for this mapping context.");
        }

        var (organizationId, groupId) = ResolveOwnerScope(payload, context);

        var draft = new CreateEventDraftRequestDto
        {
            Title = title,
            Subtitle = Normalize(payload.Subtitle),
            Description = Normalize(payload.Description),
            Content = Normalize(payload.Content),
            Slug = Normalize(payload.Slug),
            EventTypeId = payload.EventTypeId,
            AudienceGenderId = payload.AudienceGenderId,
            AudienceAgeId = payload.AudienceAgeId,
            OrganizationId = organizationId,
            GroupId = groupId,
            Price = payload.Price,
            CurrencyCode = Normalize(payload.CurrencyCode),
            IsRegistrationRequired = payload.IsRegistrationRequired,
            ExternalRegistrationUrl = Normalize(payload.ExternalRegistrationUrl),
            VisibilityTypeId = payload.VisibilityTypeId,
            EventFormatId = payload.EventFormatId,
            MadhabId = payload.MadhabId,
            Timezone = Normalize(payload.Timezone),
            EventTimeZoneId = Normalize(payload.EventTimeZoneId),
            EventUrl = Normalize(payload.EventUrl),
            CategoryIds = payload.CategoryIds.Distinct().ToList(),
            TagIds = payload.TagIds.Distinct().ToList()
        };

        return CreateEventDraftAiActionMappingResult.Success(draft);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static (Guid? OrganizationId, Guid? GroupId) ResolveOwnerScope(
        CreateEventDraftAiActionPayload payload,
        CreateEventDraftAiActionMappingContext context)
    {
        if (context.ForcePersonalOwnerScope)
        {
            return (null, null);
        }

        if (context.ForcedOrganizationId.HasValue)
        {
            return (context.ForcedOrganizationId, null);
        }

        if (context.ForcedGroupId.HasValue)
        {
            return (null, context.ForcedGroupId);
        }

        return (payload.OrganizationId, payload.GroupId);
    }

    private static bool ValidateLength(
        string? value,
        int maxLength,
        string fieldName,
        out CreateEventDraftAiActionMappingResult? failure)
    {
        failure = null;

        if (Normalize(value)?.Length > maxLength)
        {
            failure = CreateEventDraftAiActionMappingResult.Failure(
                "field_too_long",
                $"AI event draft {fieldName} exceeds the allowed length.");
            return false;
        }

        return true;
    }
}

public sealed record CreateEventDraftAiActionMappingContext(
    IReadOnlySet<Guid> AllowedOrganizationIds,
    IReadOnlySet<Guid> AllowedGroupIds)
{
    public Guid? ForcedOrganizationId { get; init; }
    public Guid? ForcedGroupId { get; init; }
    public bool ForcePersonalOwnerScope { get; init; }
    public bool HasForcedOwnerScope => ForcePersonalOwnerScope || ForcedOrganizationId.HasValue || ForcedGroupId.HasValue;

    public static CreateEventDraftAiActionMappingContext Empty { get; } = new(new HashSet<Guid>(), new HashSet<Guid>());
}

public sealed record CreateEventDraftAiActionMappingResult(
    bool Succeeded,
    CreateEventDraftRequestDto? Draft,
    string? FailureCode,
    string? FailureMessage)
{
    public static CreateEventDraftAiActionMappingResult Success(CreateEventDraftRequestDto draft)
        => new(true, draft, null, null);

    public static CreateEventDraftAiActionMappingResult Failure(string failureCode, string failureMessage)
        => new(false, null, failureCode, failureMessage);
}
