// ABOUTME: Domain entity representing an external login provider for a user.
// Stores OAuth/OIDC provider information for user authentication.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Explore.Domain.Interfaces;

namespace Explore.Domain
{
    public class UserExternalLogin : ITenantEntity
    {
        public Guid Id { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }

        public required User User { get; set; }

        [ForeignKey("Tenant")]
        public Guid TenantId { get; set; }

        public required Tenant Tenant { get; set; }

        public string? Provider { get; set; }
        public string? ProviderKey { get; set; }
        public string? ProviderDisplayName { get; set; }
    }
}
