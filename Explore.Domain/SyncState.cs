using System;

namespace Explore.Domain
{
    public class SyncState
    {
        public int Id { get; set; }
        /// <summary>
        /// Relay URL - unique
        /// </summary>
        public string Service { get; set; }
        /// <summary>
        /// Last processed sequence number
        /// </summary>
        public long Cursor { get; set; }
        public DateTime? LastSeqTime { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
