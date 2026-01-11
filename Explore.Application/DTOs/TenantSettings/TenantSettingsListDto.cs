using System;

namespace Explore.Application.DTOs.TenantSettings
{
    public class TenantSettingsListDto
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public string TenantFullName { get; set; }
    }
}
