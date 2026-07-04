// ABOUTME: Query-bound request models for shared paginated API list validation.
// ABOUTME: Converts abusive page/filter query parameters into early ApiController validation failures.

using System.ComponentModel.DataAnnotations;
using Explore.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Models;

public class PaginationQueryRequest : IValidatableObject
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => QueryValidationRules.ValidatePagination(PageNumber, PageSize);
}

public sealed class EventSeriesListQueryRequest : PaginationQueryRequest
{
    public EventSeriesListQueryRequest()
    {
        PageSize = 10;
    }

    public Guid? ActorId { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => base.Validate(validationContext)
            .Concat(QueryValidationRules.ValidateOptionalGuid(ActorId, nameof(ActorId)));
}

public sealed class EventTemplateListQueryRequest : PaginationQueryRequest
{
    public int? EventTypeId { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => base.Validate(validationContext)
            .Concat(QueryValidationRules.ValidateOptionalPositiveInt(EventTypeId, nameof(EventTypeId)));
}

public sealed class EventSessionTemplateListQueryRequest : PaginationQueryRequest
{
    public Guid EventTemplateId { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => base.Validate(validationContext)
            .Concat(QueryValidationRules.ValidateRequiredGuid(EventTemplateId, nameof(EventTemplateId)));
}

public sealed class CustomPropertyDefinitionListQueryRequest : PaginationQueryRequest
{
    public EntityTypeName EntityTypeName { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in base.Validate(validationContext))
        {
            yield return result;
        }

        if (!Enum.IsDefined(EntityTypeName))
        {
            yield return new ValidationResult(
                $"{nameof(EntityTypeName)} is not supported.",
                [nameof(EntityTypeName)]);
        }
    }
}

public sealed class EventCustomPropertyDefinitionListQueryRequest : PaginationQueryRequest
{
    public Guid EventId { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => base.Validate(validationContext)
            .Concat(QueryValidationRules.ValidateRequiredGuid(EventId, nameof(EventId)));
}

public sealed class EventSessionCustomPropertyDefinitionListQueryRequest : PaginationQueryRequest
{
    public Guid EventSessionId { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => base.Validate(validationContext)
            .Concat(QueryValidationRules.ValidateRequiredGuid(EventSessionId, nameof(EventSessionId)));
}

public sealed class NotificationListQueryRequest : PaginationQueryRequest
{
    public bool? IsRead { get; set; }

    public int? NotificationTypeId { get; set; }

    public int? NotificationScopeId { get; set; }

    public int? NotificationReasonId { get; set; }

    public bool? IsArchived { get; set; }

    public bool? IsSnoozed { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => base.Validate(validationContext)
            .Concat(QueryValidationRules.ValidateOptionalPositiveInt(
                NotificationTypeId,
                nameof(NotificationTypeId)))
            .Concat(QueryValidationRules.ValidateOptionalPositiveInt(
                NotificationScopeId,
                nameof(NotificationScopeId)))
            .Concat(QueryValidationRules.ValidateOptionalPositiveInt(
                NotificationReasonId,
                nameof(NotificationReasonId)));
}

public sealed class ContactShareConsentListQueryRequest : PaginationQueryRequest
{
    public Guid? EventId { get; set; }

    public string? EmailSearch { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => base.Validate(validationContext)
            .Concat(QueryValidationRules.ValidateOptionalGuid(EventId, nameof(EventId)))
            .Concat(QueryValidationRules.ValidateBoundedText(
                EmailSearch,
                nameof(EmailSearch),
                QueryValidationRules.MaxSearchTermLength));
}

public sealed class ContactShareConsentExportQueryRequest : IValidatableObject
{
    public string? Format { get; set; } = "csv";

    public Guid? EventId { get; set; }

    public string GetNormalizedFormat() => Format?.Trim().ToLowerInvariant() ?? "csv";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => QueryValidationRules.ValidateContactShareExportFormat(Format, nameof(Format))
            .Concat(QueryValidationRules.ValidateOptionalGuid(EventId, nameof(EventId)));
}

public sealed class CustomPropertyGovernanceReportQueryRequest : PaginationQueryRequest
{
    public Guid TenantId { get; set; }

    public string? Scope { get; set; }

    public PromotionRecommendation? Recommendation { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in base.Validate(validationContext))
        {
            yield return result;
        }

        foreach (var result in QueryValidationRules.ValidateRequiredGuid(TenantId, nameof(TenantId)))
        {
            yield return result;
        }

        foreach (var result in QueryValidationRules.ValidateBoundedText(
                     Scope,
                     nameof(Scope),
                     QueryValidationRules.MaxShortTextLength))
        {
            yield return result;
        }

        if (Recommendation.HasValue && !Enum.IsDefined(Recommendation.Value))
        {
            yield return new ValidationResult(
                $"{nameof(Recommendation)} is not supported.",
                [nameof(Recommendation)]);
        }
    }
}

public sealed class CustomPropertyProjectionDirtyScopesQueryRequest : PaginationQueryRequest
{
    public Guid TenantId { get; set; }

    public string? ProjectionName { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in base.Validate(validationContext))
        {
            yield return result;
        }

        foreach (var result in QueryValidationRules.ValidateRequiredGuid(TenantId, nameof(TenantId)))
        {
            yield return result;
        }

        if (string.IsNullOrWhiteSpace(ProjectionName))
        {
            yield return new ValidationResult(
                $"{nameof(ProjectionName)} is required.",
                [nameof(ProjectionName)]);
            yield break;
        }

        foreach (var result in QueryValidationRules.ValidateBoundedText(
                     ProjectionName,
                     nameof(ProjectionName),
                     QueryValidationRules.MaxShortTextLength))
        {
            yield return result;
        }
    }
}

public sealed class TemplateSyncHistoryQueryRequest : IValidatableObject
{
    [FromQuery(Name = "page")]
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Page < 1)
        {
            yield return new ValidationResult(
                "Page must be greater than or equal to 1.",
                [nameof(Page)]);
        }

        foreach (var result in QueryValidationRules.ValidatePagination(1, PageSize))
        {
            yield return result;
        }
    }
}
