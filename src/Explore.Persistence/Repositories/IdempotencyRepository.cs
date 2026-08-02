// ABOUTME: Repository implementation for idempotency replay persistence using ExploreDbContext.
// ABOUTME: Claims keys atomically by tenant and completes responses only from the claim owner.

using System.Data;
using System.Runtime.CompilerServices;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Explore.Persistence.Repositories;

public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly ExploreDbContext _dbContext;

    public IdempotencyRepository(ExploreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IdempotencyRecord?> FindAsync(string key, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Key == key && r.TenantId == tenantId && r.ExpiresAt > DateTime.UtcNow,
                cancellationToken);
    }

    public async Task SaveAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
    {
        await _dbContext.IdempotencyRecords.AddAsync(record, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IdempotencyClaim> TryClaimAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken = default)
    {
        if (!_dbContext.Database.IsRelational())
        {
            var current = await FindAsync(record.Key, record.TenantId, cancellationToken);
            if (current is null)
            {
                await SaveAsync(record, cancellationToken);
                return new IdempotencyClaim(record, IsOwner: true);
            }

            return new IdempotencyClaim(current, IsOwner: false);
        }

        string providerName = _dbContext.Database.ProviderName
            ?? throw new InvalidOperationException("The relational database provider is unavailable.");
        return providerName == RelationalNamedLock.SqlServerProvider
            ? await TryClaimSqlServerAsync(record, cancellationToken)
            : await ExecuteRelationalClaimAsync(providerName, record, cancellationToken);
    }

    private async Task<IdempotencyClaim> TryClaimSqlServerAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            if (_dbContext.Database.CurrentTransaction is null)
            {
                transaction = await _dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);
            }

            IdempotencyClaim claim = await ExecuteRelationalClaimAsync(
                RelationalNamedLock.SqlServerProvider,
                record,
                cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return claim;
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private async Task<IdempotencyClaim> ExecuteRelationalClaimAsync(
        string providerName,
        IdempotencyRecord record,
        CancellationToken cancellationToken)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            CreateClaimCommand(providerName, record),
            cancellationToken);

        var persisted = await _dbContext.IdempotencyRecords
            .AsNoTracking()
            .SingleAsync(
                current => current.Key == record.Key && current.TenantId == record.TenantId,
                cancellationToken);
        return new IdempotencyClaim(persisted, persisted.Id == record.Id);
    }

    private FormattableString CreateClaimCommand(string providerName, IdempotencyRecord record)
    {
        var entityType = _dbContext.Model.FindEntityType(typeof(IdempotencyRecord))
            ?? throw new InvalidOperationException("The idempotency record mapping is unavailable.");
        var tableName = entityType.GetTableName()
            ?? throw new InvalidOperationException("The idempotency record table mapping is unavailable.");
        var storeObject = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());
        var sqlHelper = _dbContext.GetService<ISqlGenerationHelper>();
        string Column(string propertyName) => sqlHelper.DelimitIdentifier(
            entityType.FindProperty(propertyName)?.GetColumnName(storeObject)
                ?? throw new InvalidOperationException($"The {propertyName} column mapping is unavailable."));

        var table = sqlHelper.DelimitIdentifier(tableName, entityType.GetSchema());
        var id = Column(nameof(IdempotencyRecord.Id));
        var key = Column(nameof(IdempotencyRecord.Key));
        var tenantId = Column(nameof(IdempotencyRecord.TenantId));
        var userId = Column(nameof(IdempotencyRecord.UserId));
        var requestMethod = Column(nameof(IdempotencyRecord.RequestMethod));
        var requestTarget = Column(nameof(IdempotencyRecord.RequestTarget));
        var requestContentType = Column(nameof(IdempotencyRecord.RequestContentType));
        var requestBodyHash = Column(nameof(IdempotencyRecord.RequestBodyHash));
        var principalFingerprint = Column(nameof(IdempotencyRecord.PrincipalFingerprint));
        var statusCode = Column(nameof(IdempotencyRecord.StatusCode));
        var responseBody = Column(nameof(IdempotencyRecord.ResponseBody));
        var contentType = Column(nameof(IdempotencyRecord.ContentType));
        var createdAt = Column(nameof(IdempotencyRecord.CreatedAt));
        var expiresAt = Column(nameof(IdempotencyRecord.ExpiresAt));
        var target = sqlHelper.DelimitIdentifier("target");

        string sql = providerName switch
        {
            RelationalNamedLock.PostgreSqlProvider or RelationalNamedLock.SqliteProvider => $$"""
                INSERT INTO {{table}} AS {{target}}
                    ({{id}}, {{key}}, {{tenantId}}, {{userId}}, {{requestMethod}}, {{requestTarget}},
                     {{requestContentType}}, {{requestBodyHash}}, {{principalFingerprint}}, {{statusCode}},
                     {{responseBody}}, {{contentType}}, {{createdAt}}, {{expiresAt}})
                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13})
                ON CONFLICT ({{key}}, {{tenantId}}) DO UPDATE SET
                    {{id}} = excluded.{{id}},
                    {{userId}} = excluded.{{userId}},
                    {{requestMethod}} = excluded.{{requestMethod}},
                    {{requestTarget}} = excluded.{{requestTarget}},
                    {{requestContentType}} = excluded.{{requestContentType}},
                    {{requestBodyHash}} = excluded.{{requestBodyHash}},
                    {{principalFingerprint}} = excluded.{{principalFingerprint}},
                    {{statusCode}} = excluded.{{statusCode}},
                    {{responseBody}} = excluded.{{responseBody}},
                    {{contentType}} = excluded.{{contentType}},
                    {{createdAt}} = excluded.{{createdAt}},
                    {{expiresAt}} = excluded.{{expiresAt}}
                WHERE {{target}}.{{expiresAt}} <= excluded.{{createdAt}}
                """,
            RelationalNamedLock.SqlServerProvider => $$"""
                UPDATE {{table}} WITH (UPDLOCK, HOLDLOCK)
                SET {{id}} = {0},
                    {{userId}} = {3},
                    {{requestMethod}} = {4},
                    {{requestTarget}} = {5},
                    {{requestContentType}} = {6},
                    {{requestBodyHash}} = {7},
                    {{principalFingerprint}} = {8},
                    {{statusCode}} = {9},
                    {{responseBody}} = {10},
                    {{contentType}} = {11},
                    {{createdAt}} = {12},
                    {{expiresAt}} = {13}
                WHERE {{key}} = {1} AND {{tenantId}} = {2} AND {{expiresAt}} <= {12};

                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO {{table}}
                        ({{id}}, {{key}}, {{tenantId}}, {{userId}}, {{requestMethod}}, {{requestTarget}},
                         {{requestContentType}}, {{requestBodyHash}}, {{principalFingerprint}}, {{statusCode}},
                         {{responseBody}}, {{contentType}}, {{createdAt}}, {{expiresAt}})
                    SELECT {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM {{table}} WITH (UPDLOCK, HOLDLOCK)
                        WHERE {{key}} = {1} AND {{tenantId}} = {2});
                END
                """,
            RelationalNamedLock.MySqlProvider => $$"""
                INSERT INTO {{table}}
                    ({{id}}, {{key}}, {{tenantId}}, {{userId}}, {{requestMethod}}, {{requestTarget}},
                     {{requestContentType}}, {{requestBodyHash}}, {{principalFingerprint}}, {{statusCode}},
                     {{responseBody}}, {{contentType}}, {{createdAt}}, {{expiresAt}})
                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13})
                ON DUPLICATE KEY UPDATE
                    {{id}} = IF({{expiresAt}} <= VALUES({{createdAt}}), VALUES({{id}}), {{id}}),
                    {{userId}} = IF({{expiresAt}} <= VALUES({{createdAt}}), VALUES({{userId}}), {{userId}}),
                    {{requestMethod}} = IF({{expiresAt}} <= VALUES({{createdAt}}), VALUES({{requestMethod}}), {{requestMethod}}),
                    {{requestTarget}} = IF({{expiresAt}} <= VALUES({{createdAt}}), VALUES({{requestTarget}}), {{requestTarget}}),
                    {{requestContentType}} = IF({{expiresAt}} <= VALUES({{createdAt}}), VALUES({{requestContentType}}), {{requestContentType}}),
                    {{requestBodyHash}} = IF({{expiresAt}} <= VALUES({{createdAt}}), VALUES({{requestBodyHash}}), {{requestBodyHash}}),
                    {{principalFingerprint}} = IF({{expiresAt}} <= VALUES({{createdAt}}), VALUES({{principalFingerprint}}), {{principalFingerprint}}),
                    {{statusCode}} = IF({{expiresAt}} <= VALUES({{createdAt}}), VALUES({{statusCode}}), {{statusCode}}),
                    {{responseBody}} = IF({{expiresAt}} <= VALUES({{createdAt}}), VALUES({{responseBody}}), {{responseBody}}),
                    {{contentType}} = IF({{expiresAt}} <= VALUES({{createdAt}}), VALUES({{contentType}}), {{contentType}}),
                    {{createdAt}} = IF({{expiresAt}} <= VALUES({{createdAt}}), VALUES({{createdAt}}), {{createdAt}}),
                    {{expiresAt}} = IF({{expiresAt}} <= VALUES({{createdAt}}), VALUES({{expiresAt}}), {{expiresAt}})
                """,
            _ => throw new InvalidOperationException(
                $"Unsupported relational idempotency provider '{providerName}'."),
        };

        return FormattableStringFactory.Create(sql,
            record.Id,
            record.Key,
            record.TenantId,
            record.UserId,
            record.RequestMethod,
            record.RequestTarget,
            record.RequestContentType,
            record.RequestBodyHash,
            record.PrincipalFingerprint,
            record.StatusCode,
            record.ResponseBody,
            record.ContentType,
            record.CreatedAt,
            record.ExpiresAt);
    }

    public async Task<bool> CompleteAsync(
        Guid recordId,
        int statusCode,
        string? responseBody,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        if (!_dbContext.Database.IsRelational())
        {
            var record = await _dbContext.IdempotencyRecords.FirstOrDefaultAsync(
                candidate => candidate.Id == recordId
                    && candidate.StatusCode == IdempotencyRecord.InProgressStatusCode,
                cancellationToken);
            if (record is null)
            {
                return false;
            }

            record.StatusCode = statusCode;
            record.ResponseBody = responseBody;
            record.ContentType = contentType;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        var updated = await _dbContext.IdempotencyRecords
            .Where(record => record.Id == recordId && record.StatusCode == IdempotencyRecord.InProgressStatusCode)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(record => record.StatusCode, statusCode)
                    .SetProperty(record => record.ResponseBody, responseBody)
                    .SetProperty(record => record.ContentType, contentType),
                cancellationToken);
        return updated == 1;
    }

    public async Task<bool> ReleaseAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        if (!_dbContext.Database.IsRelational())
        {
            var record = await _dbContext.IdempotencyRecords.FirstOrDefaultAsync(
                candidate => candidate.Id == recordId
                    && candidate.StatusCode == IdempotencyRecord.InProgressStatusCode,
                cancellationToken);
            if (record is null)
            {
                return false;
            }

            _dbContext.IdempotencyRecords.Remove(record);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        var deleted = await _dbContext.IdempotencyRecords
            .Where(record => record.Id == recordId && record.StatusCode == IdempotencyRecord.InProgressStatusCode)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted == 1;
    }

    public async Task<int> CountExpiredAsync(
        DateTime expiresBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.IdempotencyRecords
            .AsNoTracking()
            .Where(record => record.ExpiresAt <= expiresBeforeUtc)
            .OrderBy(record => record.ExpiresAt)
            .Take(batchSize)
            .CountAsync(cancellationToken);
    }

    public async Task<int> DeleteExpiredAsync(
        DateTime expiresBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var expiredRecordIds = _dbContext.IdempotencyRecords
            .Where(record => record.ExpiresAt <= expiresBeforeUtc)
            .OrderBy(record => record.ExpiresAt)
            .Select(record => record.Id)
            .Take(batchSize);

        return await _dbContext.IdempotencyRecords
            .Where(record => expiredRecordIds.Contains(record.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
