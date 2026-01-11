using System;

namespace Explore.Application.DTOs.SyncState
{
    public class CreateSyncStateDto
    {
        public string Service { get; set; }
        public long Cursor { get; set; }
        public DateTime? LastSeqTime { get; set; }
    }
}
