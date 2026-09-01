// ABOUTME: Defines persistence access for authority-qualified external login identities.
// ABOUTME: Requires canonical account keys so repositories cannot perform raw-subject fallback reads.

using Explore.Application.Authentication;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IUserExternalLoginRepository : IGenericRepository<UserExternalLogin, Guid>
{
    Task<UserExternalLogin?> GetByProviderAndKey(string provider, ProviderAccountKey accountKey);
    Task<List<UserExternalLogin>> GetByUser(Guid userId);
}
