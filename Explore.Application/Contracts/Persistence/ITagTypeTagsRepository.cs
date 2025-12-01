using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface ITagTypeTagsRepository : IGenericRepository<TagType, Guid>
    {
        Task<TagType> GetTagTypeWithTags(Guid id);
        Task<List<TagType>> GetTagTypesWithTags();
    }
}
