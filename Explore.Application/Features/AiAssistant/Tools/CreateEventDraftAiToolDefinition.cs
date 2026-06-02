// ABOUTME: Defines the governed CreateEventDraft AI tool contract for prompts and parser validation.
// ABOUTME: Keeps provider schema, allowed fields, and confirmation posture in one Application-layer source.

using Explore.Application.Authorization;
using Explore.Application.Features.AiAssistant.Actions;
using Explore.Domain.Ai;

namespace Explore.Application.Features.AiAssistant.Tools;

public static class CreateEventDraftAiToolDefinition
{
    public static AiToolDefinition Create()
        => new(
            AiProposedActionKind.CreateEventDraft,
            "CreateEventDraft",
            "Create event draft",
            JsonSchema,
            AllowedPayloadFields,
            ForbiddenPayloadFields,
            typeof(CreateEventDraftAiActionMapper),
            new AiToolAuthorizationRequirement(ResourceKinds.Event, AuthorizationActions.Create));

    public static IReadOnlySet<string> AllowedPayloadFields { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "title",
        "subtitle",
        "description",
        "content",
        "slug",
        "eventTypeId",
        "audienceGenderId",
        "audienceAgeId",
        "organizationId",
        "groupId",
        "price",
        "currencyCode",
        "isRegistrationRequired",
        "externalRegistrationUrl",
        "visibilityTypeId",
        "eventFormatId",
        "madhabId",
        "timezone",
        "eventTimeZoneId",
        "eventUrl",
        "categoryIds",
        "tagIds"
    };

    public static IReadOnlySet<string> ForbiddenPayloadFields { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "tenantId",
        "eventStatusId",
        "createdBy",
        "updatedBy",
        "publishedAt",
        "isPublished",
        "sessions"
    };

    public const string JsonSchema = """
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["title"],
          "properties": {
            "title": { "type": "string" },
            "subtitle": { "type": "string" },
            "description": { "type": "string" },
            "content": { "type": "string" },
            "slug": { "type": "string" },
            "eventTypeId": { "type": "integer" },
            "audienceGenderId": { "type": "integer" },
            "audienceAgeId": { "type": "integer" },
            "organizationId": { "type": "string", "format": "uuid" },
            "groupId": { "type": "string", "format": "uuid" },
            "price": { "type": "number", "minimum": 0 },
            "currencyCode": { "type": "string", "maxLength": 3 },
            "isRegistrationRequired": { "type": "boolean" },
            "externalRegistrationUrl": { "type": "string" },
            "visibilityTypeId": { "type": "integer" },
            "eventFormatId": { "type": "integer" },
            "madhabId": { "type": "integer" },
            "timezone": { "type": "string" },
            "eventTimeZoneId": { "type": "string" },
            "eventUrl": { "type": "string" },
            "categoryIds": { "type": "array", "items": { "type": "string", "format": "uuid" } },
            "tagIds": { "type": "array", "items": { "type": "string", "format": "uuid" } }
          }
        }
        """;
}
