using System;

namespace Explore.Application.DTOs.UserExternalLogin
{
    public class UpdateUserExternalLoginDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public string Provider { get; set; }
        public string ProviderKey { get; set; }
        public string ProviderDisplayName { get; set; }
    }
}
