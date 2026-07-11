using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface ITagTypeTagsRepository : IGenericRepository<TagTypeTags, Guid>
{
    Task<List<Tag>> GetTagsByTagType(int tagTypeId);
    Task<List<TagType>> GetTagTypesForTag(Guid tagId);
    Task<bool> Exists(Guid tagId, int tagTypeId);
    Task<List<(TagType TagType, List<Tag> Tags)>> GetAllTagsGroupedByTagType();
}
