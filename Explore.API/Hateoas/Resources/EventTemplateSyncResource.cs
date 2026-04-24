// ABOUTME: Minimal API-layer resource descriptor for event template sync affordances.
// ABOUTME: Carries route values needed by HATEOAS policies without leaking transport concerns into Application DTOs.

namespace Explore.API.Hateoas.Resources;

public sealed record EventTemplateSyncResource(Guid EventId, int TargetTemplateVersion, bool HasChanges);
