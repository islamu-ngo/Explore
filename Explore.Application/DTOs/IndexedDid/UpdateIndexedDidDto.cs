using System;

namespace Explore.Application.DTOs.IndexedDid
{
    public class UpdateIndexedDidDto
    {
        public string Did { get; set; }
        public string? Handle { get; set; }
        public string PdsHost { get; set; }
        public string? SigningKey { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastIndexedAt { get; set; }
        public DateTime? LastSeenAt { get; set; }
    }
}
