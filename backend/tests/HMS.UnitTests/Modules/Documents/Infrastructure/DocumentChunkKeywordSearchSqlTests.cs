using System.Data;
using System.Data.Common;
using FluentAssertions;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.Documents.Infrastructure;
using HMS.Modules.Documents.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace HMS.UnitTests.Modules.Documents.Infrastructure;

/// <summary>
/// Checks the SQL the keyword search sends, without a database: the connection is never
/// opened and the command is captured instead of executed. The unit-test run has no
/// PostgreSQL, and a query EF can't translate only fails when it is first executed — this
/// catches that, and pins the parts of the query its speed and ranking depend on.
/// </summary>
public class DocumentChunkKeywordSearchSqlTests
{
    private sealed class NeverOpen : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class CaptureInsteadOfExecute : DbCommandInterceptor
    {
        public string? Sql { get; private set; }

        public Dictionary<string, object?> Parameters { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Sql = command.CommandText;
            foreach (DbParameter parameter in command.Parameters)
            {
                Parameters[parameter.ParameterName] = parameter.Value;
            }

            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(new DataTable().CreateDataReader()));
        }
    }

    private static async Task<CaptureInsteadOfExecute> CaptureAsync(IReadOnlyList<string> terms, DocumentOwnerType? ownerType, Guid? ownerId, int limit)
    {
        var capture = new CaptureInsteadOfExecute();
        var options = new DbContextOptionsBuilder<DocumentsDbContext>()
            .UseNpgsql("Host=unused;Database=unused", npgsql => npgsql.UseVector())
            .AddInterceptors(new NeverOpen(), capture)
            .Options;

        await using var dbContext = new DocumentsDbContext(options);
        var hits = await new DocumentChunkRepository(dbContext).KeywordSearchAsync(terms, ownerType, ownerId, limit, CancellationToken.None);

        hits.Should().BeEmpty();
        return capture;
    }

    [Fact]
    public async Task MatchesAnyTermAgainstTheIndexedSearchVector_BestRankFirst()
    {
        var capture = await CaptureAsync(["antibiotics", "amoxicillin"], ownerType: null, ownerId: null, limit: 40);

        capture.Sql.Should().NotBeNull();
        capture.Sql.Should().Contain("search_vector @@ to_tsquery('simple', @");
        capture.Sql.Should().MatchRegex(@"ORDER BY ts_rank\(\w+\.search_vector, to_tsquery\('simple', @\w+\)\) DESC");
        capture.Sql.Should().Contain("LIMIT @");
        capture.Parameters.Values.Should().Contain("antibiotics | amoxicillin");
        capture.Parameters.Values.Should().Contain(40);
    }

    [Fact]
    public async Task JoinsToDocuments_SoADeletedDocumentsChunksNeverMatch()
    {
        var capture = await CaptureAsync(["fever"], ownerType: null, ownerId: null, limit: 10);

        capture.Sql.Should().MatchRegex(@"INNER JOIN[\s\S]*documents\.documents");
        capture.Sql.Should().Contain("is_deleted");
    }

    [Fact]
    public async Task ScopesToTheOwner_WhenOneIsGiven()
    {
        var patientId = Guid.NewGuid();

        var scoped = await CaptureAsync(["fever"], DocumentOwnerType.Patient, patientId, limit: 10);
        var unscoped = await CaptureAsync(["fever"], ownerType: null, ownerId: null, limit: 10);

        scoped.Sql.Should().Contain("owner_type =").And.Contain("owner_id =");
        scoped.Parameters.Values.Should().Contain(patientId);
        unscoped.Sql.Should().NotContain("owner_id =");
    }

    [Fact]
    public async Task SendsNothing_WhenThereAreNoTerms()
    {
        var capture = await CaptureAsync([], DocumentOwnerType.Patient, Guid.NewGuid(), limit: 10);

        capture.Sql.Should().BeNull();
    }
}
