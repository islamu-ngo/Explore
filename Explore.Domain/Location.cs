using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class Location : ITenantEntity
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public string Address { get; set; }
    public string Postcode { get; set; }
    public string Country { get; set; }
    public string City { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Timezone { get; set; }
}
