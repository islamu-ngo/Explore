// ABOUTME: Defines the tenant, event, and purpose-owned registration workflow aggregate.
// ABOUTME: Evaluates required child requirements with ALL semantics and deterministic tenant isolation.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public sealed class RegistrationWorkflow : ITenantEntity, IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    private readonly List<RegistrationRequirement> _requirements = [];

    private RegistrationWorkflow()
    {
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; set; }
    public Guid EventId { get; private set; }
    public string Purpose { get; private set; } = string.Empty;
    public IReadOnlyList<RegistrationRequirement> Requirements => _requirements;
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public Guid ConcurrencyStamp { get; set; }

    public static RegistrationWorkflow Create(
        Guid tenantId,
        Guid eventId,
        string purpose,
        DateTime createdAt) => Create(Guid.CreateVersion7(), tenantId, eventId, purpose, createdAt);

    public static RegistrationWorkflow Create(
        Guid id,
        Guid tenantId,
        Guid eventId,
        string purpose,
        DateTime createdAt)
    {
        if (id == Guid.Empty || tenantId == Guid.Empty || eventId == Guid.Empty || string.IsNullOrWhiteSpace(purpose))
        {
            throw new ArgumentException("Workflow, tenant, event, and purpose are required.");
        }

        if (createdAt == default || createdAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be a non-default UTC value.", nameof(createdAt));
        }

        return new()
        {
            Id = id,
            TenantId = tenantId,
            EventId = eventId,
            Purpose = purpose.Trim(),
            CreatedAt = createdAt
        };
    }

    public void AddRequirement(RegistrationRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        if (requirement.TenantId != TenantId || requirement.EventId != EventId ||
            requirement.RegistrationWorkflowId != Id ||
            _requirements.Any(existing => existing.Id == requirement.Id || existing.Ordinal == requirement.Ordinal))
        {
            throw new ArgumentException("Requirement must be unique and owned by this workflow.", nameof(requirement));
        }

        _requirements.Add(requirement);
    }

    public RegistrationWorkflowEvaluation Evaluate(
        RegistrationRequirementSubjectContext subject,
        IEnumerable<RegistrationChannelCompletion> channelCompletions,
        IEnumerable<Guid> skippedRequirementIds)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(channelCompletions);
        ArgumentNullException.ThrowIfNull(skippedRequirementIds);
        if (subject.TenantId != TenantId || subject.RegistrationWorkflowId != Id)
        {
            throw new ArgumentException("Evaluation subject is outside this tenant or workflow.", nameof(subject));
        }

        RegistrationChannelCompletion[] completions = channelCompletions.ToArray();
        Guid[] skipped = skippedRequirementIds.ToArray();
        if (completions.Select(value => value.RegistrationChannelId).Distinct().Count() != completions.Length ||
            completions.Any(value => value.TenantId != TenantId || value.RegistrationWorkflowId != Id ||
                _requirements.All(requirement => requirement.Id != value.RegistrationRequirementId)) ||
            skipped.Distinct().Count() != skipped.Length || skipped.Any(id => id == Guid.Empty || _requirements.All(requirement => requirement.Id != id)))
        {
            throw new ArgumentException("Evaluation inputs must be unique and owned by this workflow.");
        }

        RegistrationRequirementEvaluation[] evaluations = _requirements
            .Select(requirement => requirement.Evaluate(
                subject,
                completions.Where(value => value.RegistrationRequirementId == requirement.Id),
                skipped.Contains(requirement.Id)))
            .ToArray();
        return new(evaluations.All(value => !value.BlocksRegistration), evaluations);
    }
}

public sealed record RegistrationWorkflowEvaluation(
    bool CanFinalize,
    IReadOnlyList<RegistrationRequirementEvaluation> Requirements);
