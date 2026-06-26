// ABOUTME: Lookup table classifying the lifecycle state of an EventSession.
// ABOUTME: Seed rows are owned by EventSessionStatusConfiguration and referenced by EventSessionStatusEnum.
using System;

namespace Explore.Domain;

public class EventSessionStatus
{
    public int Id { get; set; }
    public string MasterCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
