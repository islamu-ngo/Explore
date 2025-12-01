using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Domain
{
    public class TagType // Tag Group
    {
        public Guid Id { get; set; }
        public string MasterCode { get; set; }
        public string FullName { get; set; }
    }
}
