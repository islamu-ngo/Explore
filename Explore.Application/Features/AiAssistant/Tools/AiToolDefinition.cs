// ABOUTME: Describes an AI tool contract shared by provider schemas and future adapters.
// ABOUTME: Carries allowed payload fields, schema text, and confirmation metadata for validation.

using System;
using Explore.Domain.Ai;

namespace Explore.Application.Features.AiAssistant.Tools;

public sealed record AiToolDefinition(
    AiProposedActionKind Kind,
    string Name,
    string DisplayName,
    string JsonSchema,
    IReadOnlySet<string> AllowedPayloadFields,
    IReadOnlySet<string> ForbiddenPayloadFields,
    Type? PayloadMapperType = null,
    AiToolAuthorizationRequirement? RequiredAuthorization = null,
    AiToolConfirmationMode ConfirmationMode = AiToolConfirmationMode.Required,
    bool ExposeToProvider = true,
    bool ExposeToMcp = true);
