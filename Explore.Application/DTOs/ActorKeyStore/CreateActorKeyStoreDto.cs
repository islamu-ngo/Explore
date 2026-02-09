using System;

namespace Explore.Application.DTOs.ActorKeyStore;

public class CreateActorKeyStoreDto
{
    public Guid ActorId { get; set; }
    public Guid TenantId { get; set; }
    public required string KeyPurpose { get; set; }
    public required string PrivateKeyEncrypted { get; set; }
    public required string PublicKey { get; set; }
    public bool IsActive { get; set; }
}
