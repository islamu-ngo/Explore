namespace Explore.Application.DTOs.IndexedDid
{
    public class IndexedDidListDto
    {
        public string Did { get; set; }
        public string? Handle { get; set; }
        public string PdsHost { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastIndexedAt { get; set; }
    }
}
