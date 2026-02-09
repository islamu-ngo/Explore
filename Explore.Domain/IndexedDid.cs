using System;

namespace Explore.Domain;

public class IndexedDid
{
    /// <summary>
    /// Primary key - did:plc:xxx or did:web:xxx
    /// </summary>
    public string Did { get; set; }
    /// <summary>
    /// Current handle (e.g., alice.bsky.social)
    /// </summary>
    public string? Handle { get; set; }
    /// <summary>
    /// PDS hosting this DID
    /// </summary>
    public string PdsHost { get; set; }
    /// <summary>
    /// Current signing public key
    /// </summary>
    public string? SigningKey { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastIndexedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
}
