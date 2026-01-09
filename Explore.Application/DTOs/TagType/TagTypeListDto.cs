namespace Explore.Application.DTOs.TagType
{
    public class TagTypeListDto
    {
        public int Id { get; set; }
        public string MasterCode { get; set; } // For i18n with Tolgee
        public string FullName { get; set; } // Fallback default
    }
}
