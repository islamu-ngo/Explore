// ABOUTME: Model representing a property definition within an event session template.
// ABOUTME: Mirrors event-template definition models for drawer preview rendering.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Models.EventSessionTemplates;

public class EventSessionTemplateDefinitionModel
{
    public string? Namespace { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PropertyType PropertyType { get; set; } = PropertyType.Text;
    public bool IsRequired { get; set; }
    public bool IsMulti { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public ExposureLevel ExposureLevel { get; set; } = ExposureLevel.TenantAdminOnly;
    public bool IsSearchable { get; set; }
    public bool IsFilterable { get; set; }
    public bool IsExportable { get; set; }
    public bool IsModerationRelevant { get; set; }
    public bool IsAnalyticsRelevant { get; set; }
    public bool IsSystemOwned { get; set; }
    public string? DefaultTextValue { get; set; }
    public double? DefaultNumberValue { get; set; }
    public bool? DefaultBooleanValue { get; set; }
    public DateTime? DefaultDateTimeValue { get; set; }
    public Guid? DefaultOptionId { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? RegexPattern { get; set; }
    public double? MinNumber { get; set; }
    public double? MaxNumber { get; set; }
    public DateTime? MinDateTime { get; set; }
    public DateTime? MaxDateTime { get; set; }
    public string? AllowedUrlSchemes { get; set; }
    public IList<EventSessionTemplateOptionModel> Options { get; set; } = new List<EventSessionTemplateOptionModel>();
}
