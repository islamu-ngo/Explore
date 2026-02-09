using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventTypeRepository : IGenericRepository<EventType, int>
{

}
