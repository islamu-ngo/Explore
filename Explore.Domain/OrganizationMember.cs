using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Explore.Domain
{
    public class OrganizationMember
    {
        public Guid Id { get; set; }
        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public User User { get; set; }
        [ForeignKey("Organization")]
        public Guid OrganizationId { get; set; }
        public Organization Organization { get; set; }
        
    }
}
