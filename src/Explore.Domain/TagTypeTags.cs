using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class TagTypeTags : ITenantEntity
{
    public Guid Id { get; set; }

    [ForeignKey("Tag")]
    public Guid TagId { get; set; }
    public required Tag Tag { get; set; }

    [ForeignKey("TagType")]
    public int TagTypeId { get; set; }
    public required TagType TagType { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }
}
