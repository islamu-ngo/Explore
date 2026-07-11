using System;

namespace Explore.Application.DTOs.ActorKeyStore;

public class ActorKeyStoreListDto
{
    public Guid Id { get; set; }
    public Guid ActorId { get; set; }
    public required string ActorDisplayName { get; set; }
    public required string ActorDid { get; set; }
    public Guid TenantId { get; set; }
    public required string KeyPurpose { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
