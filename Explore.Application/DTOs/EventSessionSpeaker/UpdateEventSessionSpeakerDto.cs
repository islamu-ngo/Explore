using System;

namespace Explore.Application.DTOs.EventSessionSpeaker
{
    public class UpdateEventSessionSpeakerDto
    {
        public Guid Id { get; set; }
        public Guid ActorId { get; set; }
        public Guid EventSessionId { get; set; }
        public Guid TenantId { get; set; }
    }
}
