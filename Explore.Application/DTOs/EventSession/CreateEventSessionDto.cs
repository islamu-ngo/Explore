using System;

namespace Explore.Application.DTOs.EventSession
{
    public class CreateEventSessionDto
    {
        // Event relationship
        public Guid EventId { get; set; }

        // Timing
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }

        // Location
        public Guid? LocationId { get; set; }

        // Session Details
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Slug { get; set; }

        // Attendance
        public int? MaxAudienceAttendees { get; set; }

        // Registration
        public int? RegistrationModeId { get; set; }

        // Tenant (set by system based on context)
        public Guid TenantId { get; set; }
    }
}
