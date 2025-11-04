using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.Contracts.Persistence
{
    public interface IGenericRepository<T, TKey> where T : class
    {
        Task<T?> GetById(TKey id);
        Task<IReadOnlyList<T>> GetAll();
        Task<bool> Exists(TKey id);
        Task<T> Create(T entity);
        Task Update(T entity);
        Task Delete(T entity);
    }
}
