using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventRegistrationConfiguration : IEntityTypeConfiguration<EventRegistration>
{
    public void Configure(EntityTypeBuilder<EventRegistration> builder)
    {
        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

        builder.HasOne(e => e.EventRegistrationIntent)
            .WithMany()
            .HasForeignKey(e => e.EventRegistrationIntentId)
            .OnDelete(DeleteBehavior.Cascade);

        // ===== Performance Indexes =====

        // Unique constraint: one access row per user per event session (child-level invariant).
        builder.HasIndex(e => new { e.EventSessionId, e.UserId })
            .IsUnique()
            .HasDatabaseName("ix_eventregistrations_session_user");

        // Registrations by user (my registrations)
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_eventregistrations_user");

        // Children by parent intent (to walk a user's registration intent down to concrete access rows).
        builder.HasIndex(e => e.EventRegistrationIntentId)
            .HasDatabaseName("ix_eventregistrations_intent");
    }
}
