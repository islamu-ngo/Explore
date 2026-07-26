using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IUserExternalLoginRepository : IGenericRepository<UserExternalLogin, Guid>
{
    Task<UserExternalLogin?> GetByProviderAndKey(string provider, string providerKey);
    Task<List<UserExternalLogin>> GetByUser(Guid userId);
}
