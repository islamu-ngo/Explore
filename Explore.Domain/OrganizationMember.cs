using Explore.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Domain
{
    public class OrganizationMember
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; } // Nullable for pending invites
        public Guid OrganizationId { get; set; }
        public OrganizationRole Role { get; set; } = OrganizationRole.Member;
        public string Email { get; set; } = string.Empty; // Useful for invites before user registers or just for display
        public Organization? Organization { get; set; }
        public User? User { get; set; }
    }
}
