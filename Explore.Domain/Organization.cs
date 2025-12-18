using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Explore.Domain
{
    public class Organization
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public Guid? ProfilePictureId { get; set; }
        public string? ProfilePictureUrl { get; set; } // because in list page needs to see image, could remove the id of profile picture i thing but not sure, TODO research more
        public int ApprovalStatusId { get; set; }
        public string ApprovalStatusName { get; set; }
        //public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>(); 

        // 1. Private list for EF Core to use internally
        //private readonly List<OrganizationMember> _members = new();

        // 2. Public ReadOnly wrapper for your code to Read
        //public IReadOnlyCollection<OrganizationMember> Members => _members.AsReadOnly();
    }
}
