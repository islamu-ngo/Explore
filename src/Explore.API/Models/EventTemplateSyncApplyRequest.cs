// ABOUTME: HTTP request model for applying an event template sync plan with its expected provenance base version.
// ABOUTME: Keeps the controller contract explicit without pushing transport-specific wrappers into Application.

using Explore.Application.DTOs.EventTemplateSync;

namespace Explore.API.Models;

public sealed record EventTemplateSyncApplyRequest(
    TemplateSyncPlanDto Plan,
    int BaseProvenanceVersion);
