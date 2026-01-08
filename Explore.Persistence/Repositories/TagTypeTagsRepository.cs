using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class TagTypeTagsRepository : GenericRepository<TagTypeTags, Guid>, ITagTypeTagsRepository
    {
        private readonly ExploreDbContext _dbContext;

        public TagTypeTagsRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Tag>> GetTagsByTagType(int tagTypeId)
        {
            return await _dbContext.TagTypeTags
                .Include(tt => tt.Tag)
                .Where(tt => tt.TagTypeId == tagTypeId)
                .Select(tt => tt.Tag)
                .ToListAsync();
        }

        public async Task<List<TagType>> GetTagTypesForTag(Guid tagId)
        {
            return await _dbContext.TagTypeTags
                .Include(tt => tt.TagType)
                .Where(tt => tt.TagId == tagId)
                .Select(tt => tt.TagType)
                .ToListAsync();
        }

        public async Task<bool> Exists(Guid tagId, int tagTypeId)
        {
            return await _dbContext.TagTypeTags
                .AnyAsync(tt => tt.TagId == tagId && tt.TagTypeId == tagTypeId);
        }
    }
}
