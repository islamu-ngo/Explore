using System.ComponentModel.DataAnnotations.Schema;

namespace Explore.Domain;

public class CategoryType
{
    public int Id { get; set; }
    public string MasterCode { get; set; }
    public string FullName { get; set; }
    public string? Description { get; set; }
}
