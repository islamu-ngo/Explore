using System;

namespace Explore.Application.DTOs.SyncState
{
    public class SyncStateListDto
    {
        public int Id { get; set; }
        public string Service { get; set; }
        public long Cursor { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
