// ABOUTME: Maps tenant-owned admission targets, policies, append-only events, and current state projections.
// ABOUTME: Enforces exact tenant lineage, unique event ordering, and provider-portable ticket-target state identity.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class AdmissionTargetConfiguration : IEntityTypeConfiguration<AdmissionTarget>
{
    public void Configure(EntityTypeBuilder<AdmissionTarget> builder)
    {
        builder.ToTable("admission_targets", table =>
        {
            table.HasCheckConstraint(
                "ck_admission_targets_operational_status",
                "admission_operational_status_id IN (1, 2)");
            table.HasCheckConstraint(
                "ck_admission_targets_scope_shape",
                "(admission_target_type_id = 1 AND event_day_id IS NULL AND event_session_id IS NULL AND scope_id = event_id) OR " +
                "(admission_target_type_id = 2 AND event_day_id IS NOT NULL AND event_session_id IS NULL AND scope_id = event_day_id) OR " +
                "(admission_target_type_id = 3 AND event_day_id IS NULL AND event_session_id IS NOT NULL AND scope_id = event_session_id)");
        });
        builder.Property(target => target.Id).ValueGeneratedNever();
        builder.Property(target => target.ConcurrencyStamp).IsConcurrencyToken();
        builder.Property(target => target.AdmissionOperationalStatusId)
            .HasDefaultValue((int)Explore.Domain.Enums.AdmissionOperationalStatusEnum.Active);
        builder.HasAlternateKey(target => new { target.TenantId, target.Id });
        builder.HasAlternateKey(target => new { target.TenantId, target.EventId, target.Id });
        builder.HasIndex(target => new
            {
                target.TenantId,
                target.EventId,
                target.AdmissionTargetTypeId,
                target.ScopeId
            })
            .HasDatabaseName("ux_admission_targets_scope")
            .IsUnique();
        builder.HasOne<Tenant>().WithMany()
            .HasForeignKey(target => target.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Event>().WithMany()
            .HasForeignKey(target => new { target.TenantId, target.EventId })
            .HasPrincipalKey(value => new { value.TenantId, value.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EventDay>().WithMany()
            .HasForeignKey(target => new { target.TenantId, target.EventId, target.EventDayId })
            .HasPrincipalKey(value => new { value.TenantId, value.EventId, value.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EventSession>().WithMany()
            .HasForeignKey(target => new { target.TenantId, target.EventId, target.EventSessionId })
            .HasPrincipalKey(value => new { value.TenantId, value.EventId, value.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AdmissionCheckInPolicyConfiguration : IEntityTypeConfiguration<AdmissionCheckInPolicy>
{
    public void Configure(EntityTypeBuilder<AdmissionCheckInPolicy> builder)
    {
        builder.ToTable("admission_check_in_policies", table =>
        {
            table.HasCheckConstraint(
                "ck_admission_check_in_policies_window",
                "closes_at_utc > opens_at_utc");
            table.HasCheckConstraint(
                "ck_admission_check_in_policies_maximum_entries",
                "maximum_entries > 0");
        });
        builder.Property(policy => policy.Id).ValueGeneratedNever();
        builder.Property(policy => policy.ConcurrencyStamp).IsConcurrencyToken();
        builder.Property(policy => policy.OpensAtUtc).IsRequired();
        builder.Property(policy => policy.ClosesAtUtc).IsRequired();
        builder.Property(policy => policy.MaximumEntries).IsRequired();
        builder.HasAlternateKey(policy => new { policy.TenantId, policy.Id });
        builder.HasIndex(policy => new { policy.TenantId, policy.AdmissionTargetId })
            .HasDatabaseName("ux_admission_check_in_policies_target")
            .IsUnique();
        builder.HasOne(policy => policy.Target).WithMany()
            .HasForeignKey(policy => new { policy.TenantId, policy.AdmissionTargetId })
            .HasPrincipalKey(target => new { target.TenantId, target.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AdmissionCheckInEventConfiguration : IEntityTypeConfiguration<AdmissionCheckInEvent>
{
    public void Configure(EntityTypeBuilder<AdmissionCheckInEvent> builder)
    {
        builder.ToTable("admission_check_in_events", table =>
        {
            table.HasCheckConstraint("ck_admission_check_in_events_sequence", "sequence > 0");
            table.HasCheckConstraint(
                "ck_admission_check_in_events_action",
                "admission_check_in_action_id IN (1, 2)");
            table.HasCheckConstraint(
                "ck_admission_check_in_events_authority",
                "(actor_id IS NOT NULL AND scanner_capability_id IS NULL) OR " +
                "(actor_id IS NULL AND scanner_capability_id IS NOT NULL)");
            table.HasCheckConstraint(
                "ck_admission_check_in_events_fact_shape",
                "(admission_check_in_action_id = 1 AND admission_check_in_undo_reason_code_id IS NULL AND compensated_check_in_event_id IS NULL) OR " +
                "(admission_check_in_action_id = 2 AND admission_check_in_undo_reason_code_id IN (1, 2, 3, 4) AND compensated_check_in_event_id IS NOT NULL)");
        });
        builder.Property(value => value.Id).ValueGeneratedNever();
        builder.Property(value => value.OccurredAtUtc).IsRequired();
        builder.HasAlternateKey(value => new { value.TenantId, value.Id });
        builder.HasAlternateKey(value => new
        {
            value.TenantId,
            value.AdmissionTicketId,
            value.AdmissionTargetId,
            value.Id
        });
        builder.HasIndex(value => new
            {
                value.TenantId,
                value.AdmissionTicketId,
                value.AdmissionTargetId,
                value.Sequence
            })
            .HasDatabaseName("ux_admission_check_in_events_sequence")
            .IsUnique();
        builder.HasOne<Tenant>().WithMany()
            .HasForeignKey(value => value.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdmissionTicket>().WithMany()
            .HasForeignKey(value => new { value.TenantId, value.AdmissionTicketId })
            .HasPrincipalKey(ticket => new { ticket.TenantId, ticket.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdmissionTarget>().WithMany()
            .HasForeignKey(value => new { value.TenantId, value.AdmissionTargetId })
            .HasPrincipalKey(target => new { target.TenantId, target.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdmissionScannerCapability>().WithMany()
            .HasForeignKey(value => new { value.TenantId, value.ScannerCapabilityId })
            .HasPrincipalKey(capability => new { capability.TenantId, capability.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdmissionCheckInEvent>().WithMany()
            .HasForeignKey(value => new
            {
                value.TenantId,
                value.AdmissionTicketId,
                value.AdmissionTargetId,
                value.CompensatedCheckInEventId
            })
            .HasPrincipalKey(value => new
            {
                value.TenantId,
                value.AdmissionTicketId,
                value.AdmissionTargetId,
                value.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AdmissionScannerCapabilityConfiguration
    : IEntityTypeConfiguration<AdmissionScannerCapability>
{
    public void Configure(EntityTypeBuilder<AdmissionScannerCapability> builder)
    {
        builder.ToTable("admission_scanner_capabilities", table =>
        {
            table.HasCheckConstraint(
                "ck_admission_scanner_capabilities_key_version",
                "lookup_key_version > 0");
            table.HasCheckConstraint(
                "ck_admission_scanner_capabilities_expiry",
                "expires_at > issued_at");
        });
        builder.Property(capability => capability.Id).ValueGeneratedNever();
        builder.Property(capability => capability.LookupDigest).HasMaxLength(256).IsRequired();
        builder.Property(capability => capability.DeviceLabel).HasMaxLength(128).IsRequired();
        builder.Property(capability => capability.RevocationReason).HasMaxLength(200);
        builder.Property(capability => capability.Actions).HasConversion<int>();
        builder.Property(capability => capability.ConcurrencyStamp).IsConcurrencyToken();
        builder.HasAlternateKey(capability => new { capability.TenantId, capability.Id });
        builder.HasIndex(capability => new { capability.TenantId, capability.IssueRequestId })
            .HasDatabaseName("ux_admission_scanner_capabilities_issue_request")
            .IsUnique();
        builder.HasIndex(capability => new
            {
                capability.TenantId,
                capability.LookupKeyVersion,
                capability.LookupDigest
            })
            .HasDatabaseName("ux_admission_scanner_capabilities_digest")
            .IsUnique();
        builder.HasOne<Tenant>().WithMany()
            .HasForeignKey(capability => capability.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Event>().WithMany()
            .HasForeignKey(capability => new { capability.TenantId, capability.EventId })
            .HasPrincipalKey(value => new { value.TenantId, value.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Actor>().WithMany()
            .HasForeignKey(capability => capability.IssuedByActorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Actor>().WithMany()
            .HasForeignKey(capability => capability.RevokedByActorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(capability => new { capability.TenantId, capability.AdmissionTargetId })
            .HasDatabaseName("ix_admission_scanner_capabilities_target");
        builder.HasOne<AdmissionTarget>().WithMany()
            .HasForeignKey(capability => new
            {
                capability.TenantId,
                capability.EventId,
                capability.AdmissionTargetId
            })
            .HasPrincipalKey(value => new { value.TenantId, value.EventId, value.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AdmissionCheckInStateConfiguration : IEntityTypeConfiguration<AdmissionCheckInState>
{
    public void Configure(EntityTypeBuilder<AdmissionCheckInState> builder)
    {
        builder.ToTable("admission_check_in_states", table =>
        {
            table.HasCheckConstraint(
                "ck_admission_check_in_states_counts",
                "entry_count >= 0 AND last_sequence >= 0");
        });
        builder.Property(state => state.Id).ValueGeneratedNever();
        builder.Property(state => state.ConcurrencyStamp).IsConcurrencyToken();
        builder.HasAlternateKey(state => new { state.TenantId, state.Id });
        builder.HasIndex(state => new
            {
                state.TenantId,
                state.AdmissionTicketId,
                state.AdmissionTargetId
            })
            .HasDatabaseName("ux_admission_check_in_states_ticket_target")
            .IsUnique();
        builder.HasOne<Tenant>().WithMany()
            .HasForeignKey(state => state.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdmissionTicket>().WithMany()
            .HasForeignKey(state => new { state.TenantId, state.AdmissionTicketId })
            .HasPrincipalKey(ticket => new { ticket.TenantId, ticket.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdmissionTarget>().WithMany()
            .HasForeignKey(state => new { state.TenantId, state.AdmissionTargetId })
            .HasPrincipalKey(target => new { target.TenantId, target.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdmissionCheckInEvent>().WithMany()
            .HasForeignKey(state => new
            {
                state.TenantId,
                state.AdmissionTicketId,
                state.AdmissionTargetId,
                state.ActiveCheckInEventId
            })
            .HasPrincipalKey(value => new
            {
                value.TenantId,
                value.AdmissionTicketId,
                value.AdmissionTargetId,
                value.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
