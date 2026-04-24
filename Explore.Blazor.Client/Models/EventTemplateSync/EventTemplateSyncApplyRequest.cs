// ABOUTME: HTTP request model for applying an event template sync plan with its expected provenance base version.
// ABOUTME: Manual mirror of API model. Replace with NSwag generated client model once CI regen works.

namespace Explore.Blazor.Client.Models.EventTemplateSync;

public sealed record EventTemplateSyncApplyRequest(
    TemplateSyncPlanDto Plan,
    int BaseProvenanceVersion);
