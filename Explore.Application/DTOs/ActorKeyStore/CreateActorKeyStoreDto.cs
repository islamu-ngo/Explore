using System;

namespace Explore.Application.DTOs.ActorKeyStore
{
    public class CreateActorKeyStoreDto
    {
        public Guid ActorId { get; set; }
        public Guid TenantId { get; set; }
        public string KeyPurpose { get; set; }
        public string PrivateKeyEncrypted { get; set; }
        public string PublicKey { get; set; }
        public bool IsActive { get; set; }
    }
}
