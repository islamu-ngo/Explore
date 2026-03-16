// ABOUTME: Handler for ExportSharedContactsCommand — generates CSV/TSV file and records audit trail.
// ABOUTME: Validates org approval, builds file content, persists export + export item audit records.

using System.Globalization;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ContactShareConsent;
using Explore.Application.Features.ContactShareConsents.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.ContactShareConsents.Handlers.Commands;

public class ExportSharedContactsCommandHandler : IRequestHandler<ExportSharedContactsCommand, BaseCommandResponse<SharedContactExportResultDto>>
{
    private readonly IEventContactShareConsentRepository _consentRepository;
    private readonly IEventContactShareExportRepository _exportRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ILogger<ExportSharedContactsCommandHandler> _logger;

    public ExportSharedContactsCommandHandler(
        IEventContactShareConsentRepository consentRepository,
        IEventContactShareExportRepository exportRepository,
        IActorRepository actorRepository,
        IOrganizationRepository organizationRepository,
        ILogger<ExportSharedContactsCommandHandler> logger)
    {
        _consentRepository = consentRepository;
        _exportRepository = exportRepository;
        _actorRepository = actorRepository;
        _organizationRepository = organizationRepository;
        _logger = logger;
    }

    public async Task<BaseCommandResponse<SharedContactExportResultDto>> Handle(
        ExportSharedContactsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<SharedContactExportResultDto>();

        // Validate format
        var format = request.Format.ToLowerInvariant();
        if (format is not ("csv" or "tsv"))
        {
            response.Success = false;
            response.Message = "Invalid export format. Supported formats: csv, tsv.";
            response.Errors = ["Invalid export format."];
            return response;
        }

        // Validate actor is an approved organisation
        var actor = await _actorRepository.GetById(request.RecipientActorId);
        if (actor?.OrganizationId == null)
        {
            response.Success = false;
            response.Message = "Recipient actor is not an organisation.";
            response.Errors = ["Actor is not an organisation."];
            return response;
        }

        var org = await _organizationRepository.GetById(actor.OrganizationId.Value);
        if (org == null || org.ApprovalStatusId != (int)ApprovalStatusEnum.Approved)
        {
            response.Success = false;
            response.Message = "Organisation is not approved.";
            response.Errors = ["Organisation is not approved."];
            return response;
        }

        // Fetch granted consents
        var consents = await _consentRepository.GetGrantedForExport(
            request.TenantId, request.RecipientActorId, request.EventId);

        var separator = format == "tsv" ? '\t' : ',';
        var contentType = format == "tsv" ? "text/tab-separated-values" : "text/csv";
        var extension = format == "tsv" ? "tsv" : "csv";
        var orgName = org.FullName;
        var fileName = $"shared-contacts-{orgName.Replace(" ", "-").ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMdd}.{extension}";

        // Build file content
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(separator, [
            "Email", "GrantedAtUtc", "EventId", "EventTitle",
            "OrganizationId", "OrganizationName", "PurposeCode"
        ]));

        foreach (var consent in consents)
        {
            sb.AppendLine(string.Join(separator, [
                EscapeField(consent.EmailSnapshot, separator),
                consent.GrantedAt.ToString("o", CultureInfo.InvariantCulture),
                consent.SourceEventId?.ToString() ?? "",
                EscapeField(consent.SourceEvent?.Title ?? "", separator),
                actor.OrganizationId.Value.ToString(),
                EscapeField(orgName, separator),
                consent.PurposeCode
            ]));
        }

        var fileBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();

        // Record export audit
        var export = new EventContactShareExport
        {
            TenantId = request.TenantId,
            RecipientActorId = request.RecipientActorId,
            EventId = request.EventId,
            ExportedByUserId = request.ExportedByUserId,
            Format = format,
            RowCount = consents.Count,
            CreatedAt = DateTime.UtcNow,
            Items = consents.Select(c => new EventContactShareExportItem
            {
                ConsentId = c.Id,
                EmailSnapshot = c.EmailSnapshot
            }).ToList()
        };

        export = await _exportRepository.Create(export);

        _logger.LogInformation(
            "Exported {RowCount} shared contacts for actor {ActorId} by user {UserId} in {Format} format (export {ExportId})",
            consents.Count, request.RecipientActorId, request.ExportedByUserId, format, export.Id);

        response.Success = true;
        response.Id = new SharedContactExportResultDto
        {
            ExportId = export.Id,
            RowCount = consents.Count,
            Format = format,
            FileContent = fileBytes,
            FileName = fileName,
            ContentType = contentType
        };
        response.Message = $"Exported {consents.Count} contacts.";

        return response;
    }

    private static string EscapeField(string value, char separator)
    {
        if (separator == '\t')
            return value.Replace("\t", " ").Replace("\n", " ").Replace("\r", " ");

        if (value.Contains(separator) || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}
