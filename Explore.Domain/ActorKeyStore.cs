using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Explore.Domain.Interfaces;

namespace Explore.Domain
{
    public class ActorKeyStore : ITenantEntity
    {
        public Guid Id { get; set; }

        [ForeignKey("Actor")]
        public Guid ActorId { get; set; }
        public Actor Actor { get; set; }

        [ForeignKey("Tenant")]
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public string KeyPurpose { get; set; }
        public string PrivateKeyEncrypted { get; set; }
        public string PublicKey { get; set; }
        public bool? IsActive { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
    }
}
