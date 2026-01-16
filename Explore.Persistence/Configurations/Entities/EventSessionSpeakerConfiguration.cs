using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class EventSessionSpeakerConfiguration : IEntityTypeConfiguration<EventSessionSpeaker>
    {
        public void Configure(EntityTypeBuilder<EventSessionSpeaker> builder)
        {
            builder.HasOne(e => e.Actor)
                .WithMany()
                .HasForeignKey(e => e.ActorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.EventSession)
                .WithMany()
                .HasForeignKey(e => e.EventSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
