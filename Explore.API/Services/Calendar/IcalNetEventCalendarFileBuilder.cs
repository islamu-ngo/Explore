// ABOUTME: Ical.Net-backed serializer for RFC 5545-compatible event calendar downloads.
// ABOUTME: Emits UTC VEVENT fields with stable event GUID UID and canonical URL metadata.

using Explore.Application.DTOs.Event;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

namespace Explore.API.Services.Calendar;

public sealed class IcalNetEventCalendarFileBuilder : IEventCalendarFileBuilder
{
    private const string ProductIdentifier = "-//ISLAMU//Event Platform//EN";

    public string Build(EventCalendarExportDto calendarExport, Uri canonicalUrl)
    {
        ArgumentNullException.ThrowIfNull(calendarExport);
        ArgumentNullException.ThrowIfNull(canonicalUrl);

        var calendar = new Ical.Net.Calendar
        {
            ProductId = ProductIdentifier,
            Method = "PUBLISH"
        };

        var calendarEvent = new CalendarEvent
        {
            Summary = calendarExport.Title,
            Description = BuildDescription(calendarExport, canonicalUrl),
            DtStart = new CalDateTime(calendarExport.StartsAtUtc.UtcDateTime),
            DtEnd = new CalDateTime(calendarExport.EndsAtUtc.UtcDateTime),
            DtStamp = new CalDateTime(DateTime.UtcNow),
            Uid = calendarExport.EventId.ToString("D"),
            Url = canonicalUrl
        };

        if (!string.IsNullOrWhiteSpace(calendarExport.Location))
        {
            calendarEvent.Location = calendarExport.Location;
        }

        calendar.Events.Add(calendarEvent);

        var serializer = new CalendarSerializer();
        return serializer.SerializeToString(calendar);
    }

    private static string BuildDescription(EventCalendarExportDto export, Uri canonicalUrl)
    {
        string description = string.IsNullOrWhiteSpace(export.Description)
            ? export.Title
            : export.Description.Trim();

        return $"{description}{Environment.NewLine}{Environment.NewLine}{canonicalUrl}";
    }
}
