// ABOUTME: Defines the removable PII extension of a registration participant.
// ABOUTME: Keeps participant contact data outside durable order and assignment facts.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public sealed class RegistrationParticipantPii : ITenantEntity, IAuditableEntity
{
    private RegistrationParticipantPii()
    {
    }

    private RegistrationParticipantPii(
        Guid registrationParticipantId,
        Guid tenantId,
        string? displayName,
        string? email,
        string? phone)
    {
        RegistrationParticipantId = registrationParticipantId;
        TenantId = tenantId;
        DisplayName = Normalize(displayName);
        Email = Normalize(email);
        NormalizedEmail = Email?.ToUpperInvariant();
        Phone = Normalize(phone);
    }

    public Guid RegistrationParticipantId { get; private set; }

    public Guid TenantId { get; set; }

    public RegistrationParticipant? RegistrationParticipant { get; private set; }

    public string? DisplayName { get; private set; }

    public string? Email { get; private set; }

    public string? NormalizedEmail { get; private set; }

    public string? Phone { get; private set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static RegistrationParticipantPii Create(
        Guid registrationParticipantId,
        Guid tenantId,
        string? displayName,
        string? email,
        string? phone)
    {
        if (registrationParticipantId == Guid.Empty || tenantId == Guid.Empty)
        {
            throw new ArgumentException("Participant and tenant identifiers are required.");
        }

        return new RegistrationParticipantPii(registrationParticipantId, tenantId, displayName, email, phone);
    }

    public void Update(string? displayName, string? email, string? phone)
    {
        DisplayName = Normalize(displayName);
        Email = Normalize(email);
        NormalizedEmail = Email?.ToUpperInvariant();
        Phone = Normalize(phone);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
