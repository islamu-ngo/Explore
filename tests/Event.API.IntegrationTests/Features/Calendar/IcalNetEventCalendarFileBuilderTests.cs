// ABOUTME: Unit tests for the RFC 5545 calendar export file builder used by event downloads.
// ABOUTME: Verifies stable VEVENT fields, canonical URL metadata, and UTC timestamp serialization.

using Explore.API.Services.Calendar;
using Explore.Application.DTOs.Event;

namespace Event.Api.IntegrationTests.Features.Calendar;

public class IcalNetEventCalendarFileBuilderTests
{
    [Test]
    public async Task Build_WithEventExport_EmitsVEventWithStableFields()
    {
        var eventId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var export = new EventCalendarExportDto(
            eventId,
            "Community Iftar",
            "Bring a dish to share.",
            "community-iftar",
            new DateTimeOffset(2026, 5, 1, 18, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 1, 20, 0, 0, TimeSpan.Zero),
            "Main Hall, Brussels");
        var canonicalUrl = new Uri("https://events.example.org/events/11111111-2222-3333-4444-555555555555");

        var content = new IcalNetEventCalendarFileBuilder().Build(export, canonicalUrl);

        await Assert.That(content).Contains("BEGIN:VCALENDAR");
        await Assert.That(content).Contains("VERSION:2.0");
        await Assert.That(content).Contains("BEGIN:VEVENT");
        await Assert.That(content).Contains($"UID:{eventId:D}");
        await Assert.That(content).Contains("SUMMARY:Community Iftar");
        await Assert.That(content).Contains("DTSTART:20260501T183000Z");
        await Assert.That(content).Contains("DTEND:20260501T200000Z");
        await Assert.That(content).Contains("LOCATION:Main Hall\\, Brussels");
        await Assert.That(content).Contains("URL:https://events.example.org/events/11111111-2222-3333-4444-555555555555");
    }
}
