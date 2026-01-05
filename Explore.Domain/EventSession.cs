using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Explore.Domain
{
    public class EventSession
    {
        public Guid Id { get; set; }
        [ForeignKey("Event")]
        public Guid EventId { get; set; }
        public Event Event { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        [ForeignKey("Location")]
        public Guid? LocationId { get; set; }
        public Location? Location { get; set; }
        public string? Title { get; set; }
        [ForeignKey("Tenant")]
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }
        public string? Slug { get; set; }
        public int? MaxAudienceAttendees { get; set; }
        public int? CurrentAudienceAttendees { get; set; }
        public int? RegistrationModeId { get; set; }
        public string? Description { get; set; }
    }
}
