// ABOUTME: Executes the local-address Unicode corpus on a source-model SQLite database before migration regeneration.
// ABOUTME: Keeps SQLite SQL behavior independently runnable while PostgreSQL correctly blocks on pending migrations.

namespace Event.Persistence.IntegrationTests.Repositories;

[NotInParallel("PersistenceDb")]
public sealed class LocalAddressSuggestionSqliteUnicodeTests
{
    [Test]
    public Task SourceModelExecutesCanonicalLiteralBoundaryAndOrdinalCorpus() =>
        LocalAddressSuggestionQueryTests.RunSqliteUnicodeCorpusAsync();
}
