using System;

namespace Explore.Domain
{
    public class AtprotoRecord
    {
        public Guid Id { get; set; }
        public string Did { get; set; }
        public string Collection { get; set; }
        public string RecordKey { get; set; }
        public string? Cid { get; set; }
        public string? Uri { get; set; }
        public DateTime? IndexedAt { get; set; }
    }
}
