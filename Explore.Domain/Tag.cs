using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Explore.Domain
{
    public class Tag
    {
        public Guid Id { get; set; }
        public string MasterCode { get; set; }
        public string FullName { get; set; }

        [ForeignKey("Tenant")]
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }
    }
}
