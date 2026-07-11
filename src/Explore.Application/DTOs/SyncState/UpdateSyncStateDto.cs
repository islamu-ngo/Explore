using System;

namespace Explore.Application.DTOs.SyncState;

public class UpdateSyncStateDto
{
    public int Id { get; set; }
    public required string Service { get; set; }
    public long Cursor { get; set; }
    public DateTime? LastSeqTime { get; set; }
}
