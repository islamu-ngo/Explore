using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class Location : ITenantEntity
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Country { get; set; }
    public required string City { get; set; }

    /// <summary>
    /// 1:1 extension table containing precise location PII data.
    /// </summary>
    public required LocationPii Pii { get; set; }

    [NotMapped]
    public string Address
    {
        get => Pii.Address;
        set => Pii.Address = value;
    }

    [NotMapped]
    public string Postcode
    {
        get => Pii.Postcode;
        set => Pii.Postcode = value;
    }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    [NotMapped]
    public double? Latitude
    {
        get => Pii.Latitude;
        set => Pii.Latitude = value;
    }

    [NotMapped]
    public double? Longitude
    {
        get => Pii.Longitude;
        set => Pii.Longitude = value;
    }

    public string? Timezone { get; set; }

}
