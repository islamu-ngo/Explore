// ABOUTME: EF Core mapping for domain references attached to AI assistant conversations.
// ABOUTME: Enforces typed reference identity and prevents duplicate references per conversation.

using Explore.Domain.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class AiConversationReferenceConfiguration : IEntityTypeConfiguration<AiConversationReference>
{
    public void Configure(EntityTypeBuilder<AiConversationReference> builder)
    {
        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.Kind).HasConversion<int>().IsRequired();
        builder.Property(e => e.DisplayName).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Summary).HasMaxLength(2000);

        builder.HasIndex(e => new { e.TenantId, e.ConversationId, e.Kind, e.ReferenceId })
            .IsUnique()
            .HasDatabaseName("ux_ai_conversation_references_identity");

        builder.ToTable(t =>
            t.HasCheckConstraint("ck_ai_conversation_references_kind", "kind IN (1, 2, 3, 4)"));
    }
}
