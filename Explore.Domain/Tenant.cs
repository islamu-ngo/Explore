using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Domain
{
    public class Tenant
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Slug { get; set; }
        public bool IsActive { get; set; }
    }
}
