using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.Program;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IEventTagsRepository : IGenericRepository<EventTags, Guid>
    {
        Task<List<ProgramDto>> GetProgramsByTag(Guid tagId);
        Task<List<Tag>> GetTagsByProgram(Guid programId);
        Task<bool> Exists(Guid programId, Guid tagId);
    }
}
