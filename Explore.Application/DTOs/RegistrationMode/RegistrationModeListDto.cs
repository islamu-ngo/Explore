namespace Explore.Application.DTOs.RegistrationMode
{
    public class RegistrationModeListDto
    {
        public int Id { get; set; }
        public string MasterCode { get; set; } // For i18n with Tolgee
        public string FullName { get; set; } // Fallback default
        public string? Description { get; set; }
    }
}
