using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class OrganizationReviewConfiguration : IEntityTypeConfiguration<OrganizationReview>
{
    public void Configure(EntityTypeBuilder<OrganizationReview> builder)
    {
        builder.ToTable("organization_reviews");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuidv7()");

        builder.Property(e => e.OrganizationId).HasColumnName("organization_id");
        builder.Property(e => e.EventId).HasColumnName("event_id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.ReviewerName).HasColumnName("reviewer_name").HasMaxLength(200);
        builder.Property(e => e.Rating).HasColumnName("rating");
        builder.Property(e => e.Comment).HasColumnName("comment").HasMaxLength(2000);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
    }
}
