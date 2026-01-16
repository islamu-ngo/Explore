using System;

namespace Explore.Application.DTOs.ActorKeyStore
{
    public class ActorKeyStoreListDto
    {
        public Guid Id { get; set; }
        public Guid ActorId { get; set; }
        public string ActorDisplayName { get; set; }
        public string ActorDid { get; set; }
        public Guid TenantId { get; set; }
        public string KeyPurpose { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
