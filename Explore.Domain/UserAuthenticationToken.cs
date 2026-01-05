using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Explore.Domain
{
    public class UserAuthenticationToken
    {
        public Guid Id { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public User User { get; set; }

        [ForeignKey("Tenant")]
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public string Provider { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? PdsHost { get; set; }
        public string? DpopKey { get; set; }
        public string? IdToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
