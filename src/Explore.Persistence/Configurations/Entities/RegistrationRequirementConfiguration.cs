// ABOUTME: Maps workflow-owned registration requirements and normalized policy lookup relationships.
// ABOUTME: Enforces tenant-safe lineage, deterministic ordinals, bounded metadata, and concurrency.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class RegistrationRequirementConfiguration : IEntityTypeConfiguration<RegistrationRequirement>
{
    public void Configure(EntityTypeBuilder<RegistrationRequirement> builder)
    {
        builder.ToTable("registration_requirements");
        builder.Property(requirement => requirement.Id).ValueGeneratedNever();
        builder.Property(requirement => requirement.CreatedAt).IsRequired();
        builder.Property(requirement => requirement.IsDeleted).HasDefaultValue(false);
        builder.Property(requirement => requirement.ConcurrencyStamp).IsConcurrencyToken();
        builder.HasAlternateKey(requirement => new
        {
            requirement.TenantId,
            requirement.EventId,
            requirement.RegistrationWorkflowId,
            requirement.Id
        });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(requirement => requirement.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Event>().WithMany()
            .HasForeignKey(requirement => new { requirement.TenantId, requirement.EventId })
            .HasPrincipalKey(@event => new { @event.TenantId, @event.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RegistrationWorkflow>().WithMany(workflow => workflow.Requirements)
            .HasForeignKey(requirement => new
            {
                requirement.TenantId,
                requirement.EventId,
                requirement.RegistrationWorkflowId
            })
            .HasPrincipalKey(workflow => new { workflow.TenantId, workflow.EventId, workflow.Id })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RegistrationRequirementCriticality>().WithMany()
            .HasForeignKey(requirement => requirement.CriticalityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RegistrationRequirementCompletionEffect>().WithMany()
            .HasForeignKey(requirement => requirement.CompletionEffectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RegistrationAnswerSyncMode>().WithMany()
            .HasForeignKey(requirement => requirement.AnswerSyncModeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RegistrationRequirementSubjectType>().WithMany()
            .HasForeignKey(requirement => requirement.AppliesToSubjectTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(requirement => new { requirement.RegistrationWorkflowId, requirement.Ordinal }).IsUnique();
        builder.HasIndex(requirement => new { requirement.TenantId, requirement.EventId });
    }
}
