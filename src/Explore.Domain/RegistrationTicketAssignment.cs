// ABOUTME: Defines one ticket-unit assignment against a concrete registration order line.
// ABOUTME: Makes assigned, unassigned, and deadline-bound deferred states explicit and valid by construction.

using Explore.Domain.Enums;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public sealed class RegistrationTicketAssignment : ITenantEntity, IAuditableEntity, IConcurrencyAware
{
    private RegistrationTicketAssignment()
    {
    }

    private RegistrationTicketAssignment(
        Guid id,
        Guid tenantId,
        Guid registrationOrderLineId,
        int ordinal,
        Guid? participantId,
        AssignmentStatusEnum status,
        DateTime? assignmentDeadline,
        DateTime createdAt)
    {
        Id = id;
        TenantId = tenantId;
        RegistrationOrderLineId = registrationOrderLineId;
        Ordinal = ordinal;
        ParticipantId = participantId;
        AssignmentStatusId = (int)status;
        AssignmentDeadline = assignmentDeadline;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; set; }

    public Guid RegistrationOrderLineId { get; private set; }

    public RegistrationOrderLine? RegistrationOrderLine { get; private set; }

    public Guid? ParticipantId { get; private set; }

    public RegistrationParticipant? Participant { get; private set; }

    public int Ordinal { get; private set; }

    public int AssignmentStatusId { get; private set; }

    public AssignmentStatus? AssignmentStatus { get; private set; }

    public DateTime? AssignmentDeadline { get; private set; }

    public Guid ConcurrencyStamp { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static RegistrationTicketAssignment Create(
        Guid tenantId,
        Guid registrationOrderLineId,
        int ordinal,
        Guid? participantId,
        AssignmentStatusEnum status,
        DateTime? assignmentDeadline,
        DateTime createdAt) => Create(
        Guid.CreateVersion7(), tenantId, registrationOrderLineId, ordinal, participantId, status, assignmentDeadline, createdAt);

    public static RegistrationTicketAssignment Create(
        Guid id,
        Guid tenantId,
        Guid registrationOrderLineId,
        int ordinal,
        Guid? participantId,
        AssignmentStatusEnum status,
        DateTime? assignmentDeadline,
        DateTime createdAt)
    {
        if (id == Guid.Empty || tenantId == Guid.Empty || registrationOrderLineId == Guid.Empty || participantId == Guid.Empty ||
            !Enum.IsDefined(status))
        {
            throw new ArgumentException("Ticket assignment identity and status are invalid.");
        }

        if (ordinal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal));
        }

        DateTime normalizedCreatedAt = EnsureUtc(createdAt, nameof(createdAt));
        DateTime? normalizedDeadline = assignmentDeadline.HasValue
            ? EnsureUtc(assignmentDeadline.Value, nameof(assignmentDeadline))
            : null;

        bool isValidState = status switch
        {
            AssignmentStatusEnum.Unassigned => participantId is null && normalizedDeadline is null,
            AssignmentStatusEnum.Assigned => participantId is not null && normalizedDeadline is null,
            AssignmentStatusEnum.Deferred => participantId is null && normalizedDeadline > normalizedCreatedAt,
            _ => false
        };
        if (!isValidState)
        {
            throw new ArgumentException("Ticket assignment status, participant, and deadline are inconsistent.");
        }

        return new RegistrationTicketAssignment(
            id, tenantId, registrationOrderLineId, ordinal, participantId, status, normalizedDeadline, normalizedCreatedAt);
    }

    private static DateTime EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }

        return value;
    }
}
