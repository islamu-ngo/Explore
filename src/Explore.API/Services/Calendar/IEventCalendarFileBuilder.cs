// ABOUTME: API-layer abstraction for serializing public event data into iCalendar files.
// ABOUTME: Keeps controller response handling separate from calendar serialization details.

using Explore.Application.DTOs.Event;

namespace Explore.API.Services.Calendar;

public interface IEventCalendarFileBuilder
{
    string Build(EventCalendarExportDto calendarExport, Uri canonicalUrl);
    string Build(AttendeeEventCalendarExportDto calendarExport, Uri canonicalUrl);
}
