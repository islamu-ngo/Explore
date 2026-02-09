using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class Tag : ITenantEntity
{
    public Guid Id { get; set; }
    public string MasterCode { get; set; }
    public string FullName { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }
}
