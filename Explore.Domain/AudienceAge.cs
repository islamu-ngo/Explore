using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Domain
{
    public class AudienceAge
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
    }
}
