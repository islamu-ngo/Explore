// ABOUTME: DTO returned after an export operation with audit metadata.
// ABOUTME: Contains the export ID, row count, and format for client confirmation.

namespace Explore.Application.DTOs.ContactShareConsent;

public sealed record SharedContactExportResultDto
{
    public Guid ExportId { get; init; }
    public int RowCount { get; init; }
    public string Format { get; init; } = string.Empty;
    public byte[] FileContent { get; init; } = [];
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
}
