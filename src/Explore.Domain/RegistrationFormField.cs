// ABOUTME: Defines a version-owned registration field with stable machine identity and governance.
// ABOUTME: Keeps validation, option membership, and provider-neutral cloning behind the version aggregate.

using Explore.Domain.Enums;
using Explore.Domain.Interfaces;
using Explore.Domain.Services.Registration;

namespace Explore.Domain;

public sealed class RegistrationFormField : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    private readonly List<RegistrationFormFieldOption> _options = [];

    private RegistrationFormField()
    {
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; set; }
    public Guid EventId { get; private set; }
    public Guid RegistrationFormId { get; private set; }
    public Guid RegistrationFormVersionId { get; private set; }
    public Guid RegistrationFormSectionId { get; private set; }
    public int Ordinal { get; private set; }
    public string Namespace { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public int FieldTypeId { get; private set; }
    public RegistrationFieldType? FieldType { get; private set; }
    public int RetentionPolicyId { get; private set; }
    public int OrganizerVisibilityId { get; private set; }
    public RegistrationOrganizerVisibility? OrganizerVisibility { get; private set; }
    public bool RequiresExplicitConsent { get; private set; }
    public bool IsProviderTransferAllowed { get; private set; }
    public bool IsRequired { get; private set; }
    public bool IsMulti { get; private set; }
    public int? MinLength { get; private set; }
    public int? MaxLength { get; private set; }
    public string? RegexPattern { get; private set; }
    public decimal? MinNumber { get; private set; }
    public decimal? MaxNumber { get; private set; }
    public DateTimeOffset? MinDateTime { get; private set; }
    public DateTimeOffset? MaxDateTime { get; private set; }
    public string? AllowedUrlSchemes { get; private set; }
    public IReadOnlyCollection<RegistrationFormFieldOption> Options => _options.AsReadOnly();
    public Guid ConcurrencyStamp { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public static RegistrationFormField Create(
        Guid id,
        RegistrationFormSection section,
        int ordinal,
        string @namespace,
        string key,
        string label,
        RegistrationFieldTypeEnum fieldType,
        int retentionPolicyId,
        RegistrationOrganizerVisibilityEnum organizerVisibility,
        bool requiresExplicitConsent,
        bool isProviderTransferAllowed,
        DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(section);
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Field identifier is required.", nameof(id));
        }

        if (ordinal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        FormVersionRules.ValidateGovernance(fieldType, retentionPolicyId, organizerVisibility,
            requiresExplicitConsent, isProviderTransferAllowed);
        FormVersionRules.RequireUtc(createdAt, nameof(createdAt));
        return new RegistrationFormField
        {
            Id = id,
            TenantId = section.TenantId,
            EventId = section.EventId,
            RegistrationFormId = section.RegistrationFormId,
            RegistrationFormVersionId = section.RegistrationFormVersionId,
            RegistrationFormSectionId = section.Id,
            Ordinal = ordinal,
            Namespace = FormVersionRules.NormalizeNamespace(@namespace),
            Key = FormVersionRules.NormalizeKey(key),
            Label = label.Trim(),
            FieldTypeId = (int)fieldType,
            RetentionPolicyId = retentionPolicyId,
            OrganizerVisibilityId = (int)organizerVisibility,
            RequiresExplicitConsent = requiresExplicitConsent,
            IsProviderTransferAllowed = isProviderTransferAllowed,
            CreatedAt = createdAt
        };
    }

    internal void UpdateGovernance(
        int retentionPolicyId,
        RegistrationOrganizerVisibilityEnum organizerVisibility,
        bool requiresExplicitConsent,
        bool isProviderTransferAllowed)
    {
        FormVersionRules.ValidateGovernance((RegistrationFieldTypeEnum)FieldTypeId, retentionPolicyId,
            organizerVisibility, requiresExplicitConsent, isProviderTransferAllowed);
        RetentionPolicyId = retentionPolicyId;
        OrganizerVisibilityId = (int)organizerVisibility;
        RequiresExplicitConsent = requiresExplicitConsent;
        IsProviderTransferAllowed = isProviderTransferAllowed;
    }

    internal void UpdateValidation(
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
        FormVersionRules.ValidateConstraints(minLength, maxLength, minNumber, maxNumber, minDateTime, maxDateTime);
        if (FieldTypeId == (int)RegistrationFieldTypeEnum.OpaqueExternal && isRequired)
        {
            throw new ArgumentException("Opaque external fields cannot be required.", nameof(isRequired));
        }

        IsRequired = isRequired;
        IsMulti = isMulti;
        MinLength = minLength;
        MaxLength = maxLength;
        RegexPattern = string.IsNullOrWhiteSpace(regexPattern) ? null : regexPattern.Trim();
        MinNumber = minNumber;
        MaxNumber = maxNumber;
        MinDateTime = minDateTime;
        MaxDateTime = maxDateTime;
        AllowedUrlSchemes = string.IsNullOrWhiteSpace(allowedUrlSchemes) ? null : allowedUrlSchemes.Trim();
    }

    internal void AddOption(RegistrationFormFieldOption option)
    {
        ArgumentNullException.ThrowIfNull(option);
        if (option.RegistrationFormFieldId != Id || option.RegistrationFormVersionId != RegistrationFormVersionId ||
            option.TenantId != TenantId || option.EventId != EventId)
        {
            throw new ArgumentException("Option must belong to this field, version, event, and tenant.", nameof(option));
        }

        if (_options.Any(existing => existing.Id == option.Id || existing.Ordinal == option.Ordinal || existing.Key == option.Key))
        {
            throw new ArgumentException("Option identity, key, and ordinal must be unique within the field.", nameof(option));
        }

        _options.Add(option);
    }

    internal void RetireOption(RegistrationFormFieldOption option, DateTime retiredAt)
    {
        ArgumentNullException.ThrowIfNull(option);
        if (!_options.Contains(option))
        {
            throw new ArgumentException("Option does not belong to this field.", nameof(option));
        }

        option.Retire(retiredAt);
    }

    internal RegistrationFormField CloneTo(Guid versionId, Guid sectionId)
    {
        RegistrationFormField clone = new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = TenantId,
            EventId = EventId,
            RegistrationFormId = RegistrationFormId,
            RegistrationFormVersionId = versionId,
            RegistrationFormSectionId = sectionId,
            Ordinal = Ordinal,
            Namespace = Namespace,
            Key = Key,
            Label = Label,
            FieldTypeId = FieldTypeId,
            RetentionPolicyId = RetentionPolicyId,
            OrganizerVisibilityId = OrganizerVisibilityId,
            RequiresExplicitConsent = RequiresExplicitConsent,
            IsProviderTransferAllowed = IsProviderTransferAllowed,
            IsRequired = IsRequired,
            IsMulti = IsMulti,
            MinLength = MinLength,
            MaxLength = MaxLength,
            RegexPattern = RegexPattern,
            MinNumber = MinNumber,
            MaxNumber = MaxNumber,
            MinDateTime = MinDateTime,
            MaxDateTime = MaxDateTime,
            AllowedUrlSchemes = AllowedUrlSchemes,
            CreatedAt = CreatedAt
        };
        foreach (RegistrationFormFieldOption option in _options.Where(option => !option.IsDeleted))
        {
            clone._options.Add(option.CloneTo(versionId, sectionId, clone.Id));
        }

        return clone;
    }
}
