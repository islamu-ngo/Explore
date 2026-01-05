using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Persistence.Repositories
{
    public class FileTypeRepository : GenericRepository<FileType, int>, IFileTypeRepository
    {
        public FileTypeRepository(ExploreDbContext dbContext) : base(dbContext)
        {
        }
    }
}
