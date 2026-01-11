using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Explore.Domain
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        /// <summary>
        /// Every User MUST have an associated Actor for identity in the system.
        /// Created automatically when user syncs/registers.
        /// </summary>
        [ForeignKey("Actor")]
        public Guid ActorId { get; set; }
        public Actor Actor { get; set; }

        public string? AuthProvider { get; set; }
        public string? AuthProviderId { get; set; }
        public Guid? DefaultActorId { get; set; }
        public bool? EmailVerified { get; set; }
    }
}
