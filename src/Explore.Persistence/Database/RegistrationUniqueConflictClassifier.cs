// ABOUTME: Identifies only expected registration identity races from supported database providers.
// ABOUTME: Keeps provider exception details inside Persistence while rejecting unrelated constraints.

using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Npgsql;

namespace Explore.Persistence.Database;

internal static class RegistrationUniqueConflictClassifier
{
    private const string UniqueViolationSqlState = PostgresErrorCodes.UniqueViolation;
    private const int SqliteConstraint = 19;
    private const int SqliteConstraintPrimaryKey = 1555;
    private const int SqliteConstraintUnique = 2067;
    private const int SqlServerUniqueIndexViolation = 2601;
    private const int SqlServerUniqueConstraintViolation = 2627;
    private const int MySqlDuplicateEntry = 1062;

    private static readonly string[] SubmissionIdentityConstraints =
    [
        "ux_registration_submissions_native_identity",
        "ux_registration_submissions_provider_identity",
    ];

    private static readonly string[] RevisionIdentityConstraints =
    ["ux_registration_submission_revisions_submission_revision_number"];

    private static readonly string[][] SubmissionIdentitySqliteColumns =
    [
        [
            "islamu_event_registration_submissions.tenant_id",
            "islamu_event_registration_submissions.registration_attempt_id",
            "islamu_event_registration_submissions.business_deduplication_key",
        ],
        [
            "islamu_event_registration_submissions.tenant_id",
            "islamu_event_registration_submissions.registration_provider_binding_id",
            "islamu_event_registration_submissions.provider_submission_id",
            "islamu_event_registration_submissions.provider_response_revision",
        ],
    ];

    private static readonly string[][] RevisionIdentitySqliteColumns =
    [
        [
            "islamu_event_registration_submission_revisions.tenant_id",
            "islamu_event_registration_submission_revisions.registration_submission_id",
            "islamu_event_registration_submission_revisions.revision_number",
        ],
    ];

    internal static bool IsSubmissionIdentityConflict(DbUpdateException exception) =>
        IsExpectedConflict(exception, SubmissionIdentityConstraints, SubmissionIdentitySqliteColumns);

    internal static bool IsRevisionIdentityConflict(DbUpdateException exception) =>
        IsExpectedConflict(exception, RevisionIdentityConstraints, RevisionIdentitySqliteColumns);

    private static bool IsExpectedConflict(
        DbUpdateException exception,
        string[] expectedConstraints,
        string[][] expectedSqliteColumnSets)
    {
        for (Exception? current = exception.InnerException; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres &&
                postgres.SqlState == UniqueViolationSqlState &&
                MatchesConstraint(postgres.ConstraintName, expectedConstraints))
            {
                return true;
            }

            if (current is SqliteException sqlite &&
                sqlite.SqliteErrorCode == SqliteConstraint &&
                sqlite.SqliteExtendedErrorCode is SqliteConstraintPrimaryKey or SqliteConstraintUnique &&
                MatchesSqliteColumns(sqlite.Message, expectedSqliteColumnSets))
            {
                return true;
            }

            if (current is SqlException sqlServer &&
                sqlServer.Number is SqlServerUniqueIndexViolation or SqlServerUniqueConstraintViolation &&
                MatchesQuotedConstraint(sqlServer.Message, expectedConstraints))
            {
                return true;
            }

            if (current is MySqlException mySql &&
                mySql.Number == MySqlDuplicateEntry &&
                MatchesQuotedConstraint(mySql.Message, expectedConstraints))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesConstraint(string? actual, IEnumerable<string> expected) =>
        actual is not null && expected.Any(candidate => string.Equals(
            NormalizeConstraintIdentifier(actual), NormalizeConstraintIdentifier(candidate), StringComparison.Ordinal));

    private static bool MatchesQuotedConstraint(string message, IEnumerable<string> expected) =>
        message.Split(['\'', '`', '"', '[', ']'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(actual => MatchesConstraint(actual, expected));

    private static bool MatchesSqliteColumns(string message, IEnumerable<string[]> expectedColumnSets)
    {
        const string prefix = "UNIQUE constraint failed:";
        int prefixIndex = message.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (prefixIndex < 0)
        {
            return false;
        }

        string columns = message[(prefixIndex + prefix.Length)..].Trim().TrimEnd('.', '\'', '"');
        string[] actualColumns = columns.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeSqliteColumn)
            .ToArray();

        return expectedColumnSets.Any(expectedColumns =>
        {
            var expected = new HashSet<string>(expectedColumns.Select(NormalizeSqliteColumn), StringComparer.Ordinal);
            return actualColumns.Length == expected.Count && expected.SetEquals(actualColumns);
        });
    }

    private static string NormalizeConstraintIdentifier(string identifier)
    {
        string normalized = NormalizeSqliteColumn(identifier);
        int qualifier = normalized.LastIndexOf('.');
        return qualifier < 0 ? normalized : normalized[(qualifier + 1)..];
    }

    private static string NormalizeSqliteColumn(string identifier)
    {
        string normalized = identifier.Trim().ToLowerInvariant();
        return normalized.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '.')
            ? normalized
            : string.Empty;
    }
}
