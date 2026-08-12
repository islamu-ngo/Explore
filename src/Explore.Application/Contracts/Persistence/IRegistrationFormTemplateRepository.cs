// ABOUTME: Entity-returning persistence boundary for registration-form template catalog rows.
// ABOUTME: Exposes platform-readable and tenant-isolated template reads plus tracked mutation operations.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IRegistrationFormTemplateRepository
{
    Task<IReadOnlyList<RegistrationFormTemplate>> ListAsync(CancellationToken cancellationToken);
    Task<RegistrationFormTemplate?> GetAsync(Guid templateId, CancellationToken cancellationToken);
    Task<RegistrationFormTemplate?> GetForUpdateAsync(Guid templateId, CancellationToken cancellationToken);
    Task CreateAsync(RegistrationFormTemplate template, CancellationToken cancellationToken);
}
