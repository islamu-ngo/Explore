using System;

namespace Explore.Application.DTOs.IndexedDid;

public class IndexedDidDto
{
    public required string Did { get; set; }
    public string? Handle { get; set; }
    public required string PdsHost { get; set; }
    public string? SigningKey { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastIndexedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
}
