namespace HMS.Modules.Backups.Application.Abstractions;

/// <summary>Runs pg_dump against one database and writes its output to disk.</summary>
public interface IPgDumpRunner
{
    Task<PgDumpResult> RunAsync(string connectionString, string outputFilePath, CancellationToken cancellationToken);
}

public sealed record PgDumpResult(bool Succeeded, string? ErrorOutput);
