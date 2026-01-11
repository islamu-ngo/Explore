using System;

namespace Explore.Application.DTOs.ActorKeyStore
{
    public class ActorKeyStoreDto
    {
        public Guid Id { get; set; }
        public Guid ActorId { get; set; }
        public string ActorDisplayName { get; set; }
        public string ActorDid { get; set; }
        public Guid TenantId { get; set; }
        public string TenantFullName { get; set; }
        public string KeyPurpose { get; set; }
        public string PrivateKeyEncrypted { get; set; }
        public string PublicKey { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
