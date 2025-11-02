using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Domain
{
    public class OrganizationMember
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public Guid OrganizationId { get; set; }
    }
}
