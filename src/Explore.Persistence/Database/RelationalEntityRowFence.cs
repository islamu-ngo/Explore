// ABOUTME: Builds provider-correct exclusive row fences for admission authority decisions.
// ABOUTME: Resolves mapped schemas and prefixes so every supported relational engine shares one lock protocol.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Explore.Persistence.Database;

internal static class RelationalEntityRowFence
{
    public static async Task AcquireAsync<TEntity>(
        ExploreDbContext dbContext,
        Guid tenantId,
        string keyColumnName,
        Guid key,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyColumnName);
        if (!dbContext.Database.IsRelational())
        {
            return;
        }

        if (dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Admission authority row fences require an active unit-of-work transaction.");
        }

        string providerName = dbContext.Database.ProviderName
            ?? throw new InvalidOperationException("Admission authority requires a relational provider.");
        IEntityType entityType = dbContext.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException(
                $"Admission authority entity '{typeof(TEntity).Name}' is not mapped.");
        string tableName = entityType.GetTableName()
            ?? throw new InvalidOperationException(
                $"Admission authority entity '{typeof(TEntity).Name}' has no table mapping.");
        ISqlGenerationHelper sql = dbContext.GetService<ISqlGenerationHelper>();
        string table = sql.DelimitIdentifier(tableName, entityType.GetSchema());
        string tenantColumn = sql.DelimitIdentifier("tenant_id");
        string keyColumn = sql.DelimitIdentifier(keyColumnName);

        if (providerName == RelationalNamedLock.SqliteProvider)
        {
            string update = $"UPDATE {table} SET {keyColumn} = {keyColumn} " +
                $"WHERE {tenantColumn} = {{0}} AND {keyColumn} = {{1}}";
            await dbContext.Database.ExecuteSqlRawAsync(
                update,
                [tenantId, key],
                cancellationToken);
            return;
        }

        string command = providerName switch
        {
            RelationalNamedLock.PostgreSqlProvider or RelationalNamedLock.MySqlProvider =>
                $"SELECT {keyColumn} FROM {table} WHERE {tenantColumn} = {{0}} " +
                $"AND {keyColumn} = {{1}} FOR UPDATE",
            RelationalNamedLock.SqlServerProvider =>
                $"SELECT {keyColumn} FROM {table} WITH (UPDLOCK, HOLDLOCK) " +
                $"WHERE {tenantColumn} = {{0}} AND {keyColumn} = {{1}}",
            _ => throw new InvalidOperationException(
                $"Unsupported admission authority provider '{providerName}'."),
        };
        await dbContext.Database.ExecuteSqlRawAsync(
            command,
            [tenantId, key],
            cancellationToken);
    }
}
