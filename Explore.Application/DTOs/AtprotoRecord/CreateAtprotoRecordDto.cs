using System;

namespace Explore.Application.DTOs.AtprotoRecord;

public class CreateAtprotoRecordDto
{
    public required string Did { get; set; }
    public required string Collection { get; set; }
    public required string RecordKey { get; set; }
    public string? Cid { get; set; }
    public string? Uri { get; set; }
    public DateTime? IndexedAt { get; set; }
}
