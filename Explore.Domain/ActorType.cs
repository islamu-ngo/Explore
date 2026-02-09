using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Domain;

public class ActorType
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string MasterCode { get; set; }
    public string? Description { get; set; }
}
