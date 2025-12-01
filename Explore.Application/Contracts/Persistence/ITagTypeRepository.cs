using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface ITagTypeRepository : IGenericRepository<TagType, Guid>
    {
        Task<TagType> GetTagTypeWithDetails(Guid id);
        Task<List<TagType>> GetTagTypesWithDetails();
    }
}
