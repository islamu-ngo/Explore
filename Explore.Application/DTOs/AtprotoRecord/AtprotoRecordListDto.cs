using System;

namespace Explore.Application.DTOs.AtprotoRecord
{
    public class AtprotoRecordListDto
    {
        public Guid Id { get; set; }
        public string Did { get; set; }
        public string Collection { get; set; }
        public string RecordKey { get; set; }
        public DateTime? IndexedAt { get; set; }
    }
}
