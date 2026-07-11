// ABOUTME: HTTP request model for applying an event-session template sync plan with its expected provenance base version.
// ABOUTME: Keeps the controller contract explicit without pushing transport-specific wrappers into Application.

using Explore.Application.DTOs.EventSessionTemplateSync;

namespace Explore.API.Models;

public sealed record EventSessionTemplateSyncApplyRequest(
    TemplateSyncPlanDto Plan,
    int BaseProvenanceVersion);
