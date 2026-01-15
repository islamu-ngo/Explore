using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Explore.Domain.Interfaces;

namespace Explore.Domain
{
    public class TenantUser : ITenantEntity
    {
        public Guid Id { get; set; }
        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public User User { get; set; }
        [ForeignKey("Tenant")]
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }
        [ForeignKey("UserRole")]
        public int UserRoleId { get; set; }
        public UserRole UserRole { get; set; }
    }
}
