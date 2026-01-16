using System;

namespace Explore.Application.DTOs.Location
{
    public class CreateLocationDto
    {
        public string FullName { get; set; }
        public string Address { get; set; }
        public string Postcode { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Timezone { get; set; }
        public Guid TenantId { get; set; }
    }
}
