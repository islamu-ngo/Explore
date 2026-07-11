// ABOUTME: Contract for transactional template-to-event instantiation and provenance matching.
// ABOUTME: Creates in-memory runtime definitions/options from a template; handler persists via IUnitOfWork.

using Explore.Domain;

namespace Explore.Application.Contracts.Services;

public interface IEventTemplateInstantiationService
{
    InstantiationResult InstantiateFromTemplate(
        Guid eventId,
        Guid tenantId,
        EventTemplate template,
        string userId);

    IReadOnlyList<ProvenanceMatch> MatchByProvenance(
        IReadOnlyCollection<EventCustomPropertyDefinition> existingDefinitions,
        IReadOnlyCollection<EventTemplateCustomPropertyDefinition> templateDefinitions);
}

public sealed record InstantiationResult(
    IReadOnlyList<RuntimeDefinitionWithOptions> Definitions);

public sealed record RuntimeDefinitionWithOptions(
    EventCustomPropertyDefinition Definition,
    IReadOnlyList<EventCustomPropertyOption> Options,
    Guid? DefaultOptionId,
    EventCustomPropertyValue? DefaultValue);

public sealed record ProvenanceMatch(
    EventCustomPropertyDefinition ExistingDefinition,
    EventTemplateCustomPropertyDefinition TemplateDefinition,
    ProvenanceMatchType MatchType);

public enum ProvenanceMatchType
{
    SourceId = 1,
    NamespaceKey = 2
}
