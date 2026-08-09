// ABOUTME: EF Core repository for registration-provider connections and bindings.
// ABOUTME: Returns tracked entities for write flows and keeps mapping/entity composition inside Persistence.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class RegistrationProviderRepository(ExploreDbContext dbContext) : IRegistrationProviderRepository
{
    public Task<RegistrationProviderConnection?> GetConnectionAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken) =>
        dbContext.RegistrationProviderConnections.FirstOrDefaultAsync(connection => connection.TenantId == tenantId && connection.Id == connectionId, cancellationToken);

    public Task<RegistrationProviderBinding?> GetBindingAsync(Guid tenantId, Guid bindingId, CancellationToken cancellationToken) =>
        dbContext.RegistrationProviderBindings
            .Include(binding => binding.FieldMappings)
            .Include(binding => binding.OptionMappings)
            .Include(binding => binding.Capabilities)
            .FirstOrDefaultAsync(binding => binding.TenantId == tenantId && binding.Id == bindingId, cancellationToken);

    public Task<RegistrationProviderBinding?> GetBindingForCallbackAsync(Guid bindingId, CancellationToken cancellationToken) =>
        dbContext.RegistrationProviderBindings
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .Include(binding => binding.FieldMappings)
            .Include(binding => binding.OptionMappings)
            .Include(binding => binding.Capabilities)
            .FirstOrDefaultAsync(binding => !binding.IsDeleted && binding.Id == bindingId, cancellationToken);

    public Task<bool> HasSubmissionForBindingAsync(Guid tenantId, Guid bindingId, CancellationToken cancellationToken) =>
        dbContext.RegistrationSubmissions.AsNoTracking()
            .AnyAsync(submission => submission.TenantId == tenantId && submission.RegistrationProviderBindingId == bindingId, cancellationToken);

    public async Task AddConnectionAsync(RegistrationProviderConnection connection, CancellationToken cancellationToken) =>
        await dbContext.RegistrationProviderConnections.AddAsync(connection, cancellationToken);

    public async Task AddBindingAsync(RegistrationProviderBinding binding, CancellationToken cancellationToken) =>
        await dbContext.RegistrationProviderBindings.AddAsync(binding, cancellationToken);

    public async Task AddSchemaRevisionAsync(RegistrationProviderSchemaRevision revision, CancellationToken cancellationToken) =>
        await dbContext.RegistrationProviderSchemaRevisions.AddAsync(revision, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
