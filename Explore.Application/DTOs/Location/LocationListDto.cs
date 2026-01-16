using System;

namespace Explore.Application.DTOs.Location
{
    public class LocationListDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string? Timezone { get; set; }
    }
}
