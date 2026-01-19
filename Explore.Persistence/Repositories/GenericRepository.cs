using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Explore.Persistence.Repositories
{
    public class GenericRepository<T, TKey> : IGenericRepository<T, TKey> where T : class
    {
        private readonly ExploreDbContext _dbContext;

        public GenericRepository(ExploreDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<T> Create(T entity)
        {
            try
            {
                await _dbContext.AddAsync(entity);
                await _dbContext.SaveChangesAsync();
                return entity;
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                // Duplicate key violation - detach the entity and rethrow with more context
                _dbContext.Entry(entity).State = EntityState.Detached;
                throw new InvalidOperationException(
                    $"A record with the same unique key already exists. Constraint: {pgEx.ConstraintName}. " +
                    $"Detail: {pgEx.Detail}", 
                    ex);
            }
        }

        /// <summary>
        /// Deletes an entity. If entity implements ISoftDeletable, performs soft delete (sets IsDeleted=true).
        /// Otherwise performs hard delete (permanent removal from database).
        /// </summary>
        public async Task Delete(T entity)
        {
            // Check if entity supports soft delete
            if (entity is ISoftDeletable)
            {
                // Soft delete: Mark entity as deleted (SaveChangesAsync override handles the rest)
                _dbContext.Entry(entity).State = EntityState.Deleted;
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                // Hard delete: Permanently remove from database
                _dbContext.Set<T>().Remove(entity);
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Performs a hard delete (permanent removal from database) regardless of ISoftDeletable.
        /// Use with caution - this operation is irreversible.
        /// Should only be used by system administrators for data cleanup.
        /// </summary>
        public async Task HardDelete(T entity)
        {
            _dbContext.Set<T>().Remove(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> Exists(TKey id)
        {
            var entity = await GetById(id);
            return entity != null;
        }

        public async Task<IReadOnlyList<T>> GetAll()
        {
            return await _dbContext.Set<T>().ToListAsync();
        }

        public async Task<(IReadOnlyList<T> Items, int TotalCount)> GetAllPaged(int pageNumber, int pageSize)
        {
            var query = _dbContext.Set<T>();
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, totalCount);
        }

        public async Task<T?> GetById(TKey id)
        {
            return await _dbContext.Set<T>().FindAsync(id);
        }

        public async Task Update(T entity)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }
    }
}
