// ABOUTME: DTO returned after an export operation with audit metadata.
// ABOUTME: Contains the export ID, row count, and format for client confirmation.

namespace Explore.Application.DTOs.ContactShareConsent;

public class SharedContactExportResultDto
{
    public Guid ExportId { get; set; }
    public int RowCount { get; set; }
    public string Format { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = [];
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}
