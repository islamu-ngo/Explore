// ABOUTME: Event-local Layer 3 custom-property definition materialized from a template or created directly on the event.
// ABOUTME: Event runtime reads use these definitions, not template rows, and track provenance for supportable sync behavior.

using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Enums;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class EventCustomPropertyDefinition : ITenantEntity, IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; }

    [ForeignKey(nameof(Event))]
    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    [ForeignKey(nameof(Tenant))]
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public required string Namespace { get; set; }
    public required string Key { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public PropertyType PropertyType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsMulti { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public ExposureLevel ExposureLevel { get; set; }
    public bool IsSearchable { get; set; }
    public bool IsFilterable { get; set; }
    public bool IsExportable { get; set; }
    public bool IsModerationRelevant { get; set; }
    public bool IsAnalyticsRelevant { get; set; }
    public bool IsSystemOwned { get; set; }
    public string? DefaultTextValue { get; set; }
    public decimal? DefaultNumberValue { get; set; }
    public bool? DefaultBooleanValue { get; set; }
    public DateTimeOffset? DefaultDateTimeValue { get; set; }

    [ForeignKey(nameof(DefaultOption))]
    public Guid? DefaultOptionId { get; set; }
    public EventCustomPropertyOption? DefaultOption { get; set; }

    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? RegexPattern { get; set; }
    public decimal? MinNumber { get; set; }
    public decimal? MaxNumber { get; set; }
    public DateTimeOffset? MinDateTime { get; set; }
    public DateTimeOffset? MaxDateTime { get; set; }
    public string? AllowedUrlSchemes { get; set; }

    [ForeignKey(nameof(SourceTemplate))]
    public Guid? SourceTemplateId { get; set; }
    public EventTemplate? SourceTemplate { get; set; }

    public string? SourceTemplateKey { get; set; }
    public int? SourceTemplateVersion { get; set; }
    public Guid? SourceTemplateDefinitionId { get; set; }
    public DateTimeOffset InstantiatedAt { get; set; }
    public DateTimeOffset? LastSyncedFromTemplateAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    private readonly List<EventCustomPropertyOption> _options = [];
    public IReadOnlyCollection<EventCustomPropertyOption> Options => _options.AsReadOnly();

    private readonly List<EventCustomPropertyValue> _values = [];
    public IReadOnlyCollection<EventCustomPropertyValue> Values => _values.AsReadOnly();
}
