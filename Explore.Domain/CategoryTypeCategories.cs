using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class CategoryTypeCategories : ITenantEntity
{
    public Guid Id { get; set; }

    [ForeignKey("Category")]
    public Guid CategoryId { get; set; }
    public required Category Category { get; set; }

    [ForeignKey("CategoryType")]
    public int CategoryTypeId { get; set; }
    public required CategoryType CategoryType { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }
}
