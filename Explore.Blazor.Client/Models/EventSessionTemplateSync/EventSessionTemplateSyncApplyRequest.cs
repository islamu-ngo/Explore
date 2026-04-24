// ABOUTME: HTTP request model for applying an event session template sync plan with its expected provenance base version.
// ABOUTME: Manual mirror of API model. Replace with NSwag generated client model once CI regen works.

namespace Explore.Blazor.Client.Models.EventSessionTemplateSync;

public sealed record EventSessionTemplateSyncApplyRequest(
    TemplateSyncPlanDto Plan,
    int BaseProvenanceVersion);
