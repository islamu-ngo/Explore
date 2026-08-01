// ABOUTME: Maps requirement-owned native and provider-bound registration channels.
// ABOUTME: Enforces tenant-safe lineage, deterministic ordinals, soft deletion, and concurrency.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class RegistrationChannelConfiguration : IEntityTypeConfiguration<RegistrationChannel>
{
    public void Configure(EntityTypeBuilder<RegistrationChannel> builder)
    {
        builder.ToTable("registration_channels");
        builder.Property(channel => channel.Id).ValueGeneratedNever();
        builder.Property(channel => channel.CreatedAt).IsRequired();
        builder.Property(channel => channel.IsDeleted).HasDefaultValue(false);
        builder.Property(channel => channel.ConcurrencyStamp).IsConcurrencyToken();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(channel => channel.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Event>().WithMany()
            .HasForeignKey(channel => new { channel.TenantId, channel.EventId })
            .HasPrincipalKey(@event => new { @event.TenantId, @event.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RegistrationRequirement>().WithMany(requirement => requirement.Channels)
            .HasForeignKey(channel => new
            {
                channel.TenantId,
                channel.EventId,
                channel.RegistrationWorkflowId,
                channel.RegistrationRequirementId
            })
            .HasPrincipalKey(requirement => new
            {
                requirement.TenantId,
                requirement.EventId,
                requirement.RegistrationWorkflowId,
                requirement.Id
            })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(channel => new { channel.RegistrationRequirementId, channel.Ordinal }).IsUnique();
        builder.HasIndex(channel => new { channel.TenantId, channel.EventId });
        builder.HasIndex(channel => channel.RegistrationProviderBindingId);
    }
}
