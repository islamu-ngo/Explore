// ABOUTME: Minimal API-layer resource descriptor for event-session template sync affordances.
// ABOUTME: Carries route values needed by HATEOAS policies without leaking transport concerns into Application DTOs.

namespace Explore.API.Hateoas.Resources;

public sealed record EventSessionTemplateSyncResource(Guid SessionId, int TargetTemplateVersion, bool HasChanges);
