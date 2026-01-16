using System;

namespace Explore.Application.DTOs.UserExternalLogin
{
    public class UserExternalLoginListDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserEmail { get; set; }
        public Guid TenantId { get; set; }
        public string Provider { get; set; }
        public string ProviderDisplayName { get; set; }
    }
}
