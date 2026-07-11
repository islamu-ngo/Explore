// ABOUTME: Individual item within an export audit record, linking to the specific consent row exported.
// ABOUTME: Stores the email snapshot that was included in the export for full audit traceability.

using System.ComponentModel.DataAnnotations.Schema;

namespace Explore.Domain;

public class EventContactShareExportItem
{
    [ForeignKey("Export")]
    public Guid ExportId { get; set; }
    public EventContactShareExport? Export { get; set; }

    [ForeignKey("Consent")]
    public Guid ConsentId { get; set; }
    public EventContactShareConsent? Consent { get; set; }

    /// <summary>
    /// Snapshot of the email at the time of export — matches the consent record's snapshot.
    /// </summary>
    public required string EmailSnapshot { get; set; }
}
