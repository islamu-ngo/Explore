// ABOUTME: Owns one immutable-on-publication registration-form version and its complete field graph.
// ABOUTME: Enforces draft-only mutations, publication retirement, provenance, and independent draft cloning.

using Explore.Domain.Enums;
using Explore.Domain.Interfaces;
using Explore.Domain.Services.Registration;

namespace Explore.Domain;

public sealed class RegistrationFormVersion : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    private readonly List<RegistrationFormSection> _sections = [];

    private RegistrationFormVersion()
    {
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; set; }
    public Guid EventId { get; private set; }
    public Guid RegistrationFormId { get; private set; }
    public int Version { get; private set; }
    public int StatusId { get; private set; }
    public RegistrationFormStatus? Status { get; private set; }
    public string LanguageTag { get; private set; } = string.Empty;
    public string? SchemaHash { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime? RetiredAt { get; private set; }
    public Guid? SourceTemplateFormId { get; private set; }
    public Guid? SourceTemplateVersionId { get; private set; }
    public IReadOnlyCollection<RegistrationFormSection> Sections => _sections.AsReadOnly();
    public Guid ConcurrencyStamp { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public static RegistrationFormVersion Create(
        RegistrationForm form,
        int version,
        string languageTag,
        Guid? sourceTemplateFormId,
        Guid? sourceTemplateVersionId,
        DateTime createdAt) => Create(
            Guid.CreateVersion7(), form, version, languageTag, sourceTemplateFormId, sourceTemplateVersionId, createdAt);

    public static RegistrationFormVersion Create(
        Guid id,
        RegistrationForm form,
        int version,
        string languageTag,
        Guid? sourceTemplateFormId,
        Guid? sourceTemplateVersionId,
        DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(form);
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Version identifier is required.", nameof(id));
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if ((sourceTemplateFormId is null) != (sourceTemplateVersionId is null) ||
            sourceTemplateFormId == Guid.Empty || sourceTemplateVersionId == Guid.Empty)
        {
            throw new ArgumentException("Template form and version provenance must be supplied together.");
        }

        FormVersionRules.RequireUtc(createdAt, nameof(createdAt));
        return new RegistrationFormVersion
        {
            Id = id,
            TenantId = form.TenantId,
            EventId = form.EventId,
            RegistrationFormId = form.Id,
            Version = version,
            StatusId = (int)RegistrationFormStatusEnum.Draft,
            LanguageTag = FormVersionRules.NormalizeLanguageTag(languageTag),
            SourceTemplateFormId = sourceTemplateFormId,
            SourceTemplateVersionId = sourceTemplateVersionId,
            CreatedAt = createdAt
        };
    }

    public void AddSection(RegistrationFormSection section)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(section);
        if (section.RegistrationFormVersionId != Id || section.TenantId != TenantId || section.EventId != EventId)
        {
            throw new ArgumentException("Section must belong to this version, event, and tenant.", nameof(section));
        }

        if (_sections.Any(existing => existing.Id == section.Id || existing.Ordinal == section.Ordinal))
        {
            throw new ArgumentException("Section identity and ordinal must be unique within the version.", nameof(section));
        }

        _sections.Add(section);
    }

    public void RenameSection(RegistrationFormSection section, string title)
    {
        EnsureDraft();
        EnsureContains(section);
        section.Rename(title);
    }

    public void AddField(RegistrationFormSection section, RegistrationFormField field)
    {
        EnsureDraft();
        EnsureContains(section);
        section.AddField(field);
    }

    public void UpdateFieldGovernance(
        RegistrationFormField field,
        int retentionPolicyId,
        RegistrationOrganizerVisibilityEnum organizerVisibility,
        bool requiresExplicitConsent,
        bool isProviderTransferAllowed)
    {
        EnsureDraft();
        EnsureContains(field);
        field.UpdateGovernance(retentionPolicyId, organizerVisibility, requiresExplicitConsent, isProviderTransferAllowed);
    }

    public void UpdateFieldValidation(
        RegistrationFormField field,
        bool isRequired,
        bool isMulti,
        int? minLength,
        int? maxLength,
        string? regexPattern,
        decimal? minNumber,
        decimal? maxNumber,
        DateTimeOffset? minDateTime,
        DateTimeOffset? maxDateTime,
        string? allowedUrlSchemes)
    {
        EnsureDraft();
        EnsureContains(field);
        field.UpdateValidation(isRequired, isMulti, minLength, maxLength, regexPattern, minNumber, maxNumber,
            minDateTime, maxDateTime, allowedUrlSchemes);
    }

    public void AddOption(RegistrationFormField field, RegistrationFormFieldOption option)
    {
        EnsureDraft();
        EnsureContains(field);
        field.AddOption(option);
    }

    public void RetireOption(RegistrationFormField field, RegistrationFormFieldOption option, DateTime retiredAt)
    {
        EnsureDraft();
        EnsureContains(field);
        field.RetireOption(option, retiredAt);
    }

    public void Publish(string schemaHash, DateTime publishedAt)
    {
        EnsureDraft();
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaHash);
        FormVersionRules.RequireUtc(publishedAt, nameof(publishedAt));
        if (_sections.Count == 0 || _sections.SelectMany(section => section.Fields).Any(field =>
                field.FieldTypeId == (int)RegistrationFieldTypeEnum.OpaqueExternal && field.IsRequired))
        {
            throw new InvalidOperationException("Published forms require content and cannot require opaque external fields.");
        }

        SchemaHash = schemaHash.Trim();
        PublishedAt = publishedAt;
        StatusId = (int)RegistrationFormStatusEnum.Published;
        ConcurrencyStamp = Guid.CreateVersion7();
    }

    public void Retire(DateTime retiredAt)
    {
        if (StatusId != (int)RegistrationFormStatusEnum.Published)
        {
            throw new InvalidOperationException("Only a published form version can be retired.");
        }

        FormVersionRules.RequireUtc(retiredAt, nameof(retiredAt));
        RetiredAt = retiredAt;
        StatusId = (int)RegistrationFormStatusEnum.Retired;
        ConcurrencyStamp = Guid.CreateVersion7();
    }

    public RegistrationFormVersion CloneToDraft(int version, DateTime createdAt)
    {
        if (StatusId != (int)RegistrationFormStatusEnum.Published)
        {
            throw new InvalidOperationException("Only a published form version can be cloned into a draft.");
        }

        RegistrationFormVersion clone = new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = TenantId,
            EventId = EventId,
            RegistrationFormId = RegistrationFormId,
            Version = version,
            StatusId = (int)RegistrationFormStatusEnum.Draft,
            LanguageTag = LanguageTag,
            SourceTemplateFormId = SourceTemplateFormId,
            SourceTemplateVersionId = SourceTemplateVersionId,
            CreatedAt = createdAt
        };
        FormVersionRules.RequireUtc(createdAt, nameof(createdAt));
        if (version <= Version)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "A cloned draft must have a later version number.");
        }

        foreach (RegistrationFormSection section in _sections.Where(section => !section.IsDeleted))
        {
            clone._sections.Add(section.CloneTo(clone.Id));
        }

        return clone;
    }

    private void EnsureDraft()
    {
        if (StatusId != (int)RegistrationFormStatusEnum.Draft)
        {
            throw new InvalidOperationException("Published or retired form versions are immutable.");
        }
    }

    private void EnsureContains(RegistrationFormSection section)
    {
        ArgumentNullException.ThrowIfNull(section);
        if (!_sections.Contains(section))
        {
            throw new ArgumentException("Section does not belong to this version.", nameof(section));
        }
    }

    private void EnsureContains(RegistrationFormField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        if (!_sections.Any(section => section.Fields.Contains(field)))
        {
            throw new ArgumentException("Field does not belong to this version.", nameof(field));
        }
    }
}
