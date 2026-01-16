using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.AudienceAge
{
    public class AudienceAgeListDto
    {
        public int Id { get; set; }
        public string MasterCode { get; set; }
        public string FullName { get; set; }
        public string? Description { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
    }
}
