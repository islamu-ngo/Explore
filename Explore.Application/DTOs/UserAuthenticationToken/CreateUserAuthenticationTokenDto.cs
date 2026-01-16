using System;

namespace Explore.Application.DTOs.UserAuthenticationToken
{
    public class CreateUserAuthenticationTokenDto
    {
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public string Provider { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string PdsHost { get; set; }
        public string DpopKey { get; set; }
        public string IdToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
