// ABOUTME: Builds validated tenant-local import plans from one canonical inbound ATProto event projection.
// ABOUTME: Keeps Jetstream and bounded PDS recovery on the same mapping and validation path.

using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Application.Features.Federation.Atproto.Validators;
using Explore.Application.Services.Federation;
using Explore.Domain;
using Explore.Domain.Federation;
using FluentValidation;

namespace Explore.Application.Features.Federation.Atproto.Services;

public static class AtprotoFederatedEventImportPlanFactory
{
    public static async Task<IReadOnlyList<AtprotoFederatedEventImportPlan>> CreateAsync(
        AtprotoRecord record,
        AtprotoEventProjection projection,
        IEnumerable<Guid> tenantIds,
        CancellationToken cancellationToken)
    {
        if (record.Id == Guid.Empty
            || projection.AtprotoRecordId != record.Id
            || string.IsNullOrWhiteSpace(record.Did)
            || string.IsNullOrWhiteSpace(record.Collection)
            || string.IsNullOrWhiteSpace(record.RecordKey))
        {
            throw new ValidationException("The canonical ATProto event identity is invalid.");
        }

        var importInput = new AtprotoFederatedEventImportInput(
            projection.Name,
            projection.CreatedAt)
        {
            Description = NormalizeOptional(projection.Description),
            SourceUrl = projection.SourceUrl,
            StartsAt = projection.StartsAt,
            EndsAt = projection.EndsAt,
            Mode = NormalizeToken(projection.Mode),
            Status = NormalizeToken(projection.Status),
            RsvpExpected = projection.RsvpExpected
        };
        var validator = new AtprotoFederatedEventImportInputValidator();
        await validator.ValidateAndThrowAsync(importInput, cancellationToken);

        string atUri = string.IsNullOrWhiteSpace(record.Uri)
            ? $"at://{record.Did}/{record.Collection}/{record.RecordKey}"
            : record.Uri;
        return tenantIds
            .Distinct()
            .Select(tenantId => new AtprotoFederatedEventImportPlan(
                tenantId,
                record.Id,
                record.Did,
                atUri,
                importInput.Name.Trim(),
                importInput.CreatedAt!.Value,
                importInput.Description,
                AtprotoExternalUriPolicy.Normalize(importInput.SourceUrl),
                importInput.StartsAt,
                importInput.EndsAt,
                importInput.Mode,
                importInput.Status,
                importInput.RsvpExpected))
            .ToArray();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Trim();
        return normalized.StartsWith('#') ? normalized : $"#{normalized}";
    }
}
