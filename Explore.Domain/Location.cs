using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class Location : ITenantEntity
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public string Country { get; set; }
    public string City { get; set; }

    /// <summary>
    /// 1:1 extension table containing precise location PII data.
    /// </summary>
    public LocationPii? Pii { get; set; }

    [NotMapped]
    public string Address
    {
        get => Pii?.Address ?? null!;
        set
        {
            EnsurePii();
            Pii!.Address = value;
        }
    }

    [NotMapped]
    public string Postcode
    {
        get => Pii?.Postcode ?? null!;
        set
        {
            EnsurePii();
            Pii!.Postcode = value;
        }
    }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    [NotMapped]
    public double? Latitude
    {
        get => Pii?.Latitude;
        set
        {
            EnsurePii();
            Pii!.Latitude = value;
        }
    }

    [NotMapped]
    public double? Longitude
    {
        get => Pii?.Longitude;
        set
        {
            EnsurePii();
            Pii!.Longitude = value;
        }
    }

    public string? Timezone { get; set; }

    private void EnsurePii()
    {
        Pii ??= new LocationPii
        {
            Location = this,
            Address = null!,
            Postcode = null!
        };
    }
}
