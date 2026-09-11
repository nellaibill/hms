using System.ComponentModel;
using System.Diagnostics;
using HMS.Modules.Backups.Application.Abstractions;
using HMS.Modules.Backups.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace HMS.Modules.Backups.Infrastructure;

/// <summary>
/// Shells out to pg_dump as a real OS process rather than reimplementing a Postgres dump —
/// the same custom-format (-Fc) output `pg_restore` (and every standard Postgres tool)
/// already knows how to read. The connection string is decomposed into individual -h/-p/-U/-d
/// arguments with the password passed through the child process's environment
/// (PGPASSWORD), never on the command line — a command line is visible to every other process
/// on the same machine (Task Manager, `ps`, process-launch audit logs); an environment
/// variable set only on this one child process is not.
/// </summary>
internal sealed class PgDumpRunner : IPgDumpRunner
{
    private readonly BackupOptions _options;
    private readonly ILogger<PgDumpRunner> _logger;

    public PgDumpRunner(IOptions<BackupOptions> options, ILogger<PgDumpRunner> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PgDumpResult> RunAsync(string connectionString, string outputFilePath, CancellationToken cancellationToken)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        var outputDirectory = Path.GetDirectoryName(outputFilePath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var startInfo = BuildStartInfo(builder, _options.PgDumpExecutablePath, outputFilePath);

        Process process;
        try
        {
            process = Process.Start(startInfo) ?? throw new InvalidOperationException("pg_dump did not start.");
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            // The overwhelmingly likely cause is BackupOptions:PgDumpExecutablePath pointing at
            // nothing — surfaced as a clear, actionable log line rather than an unhandled
            // exception bubbling out of a background service.
            _logger.LogError(
                ex,
                "Failed to start pg_dump at '{Path}' — is it installed, and does BackupOptions:PgDumpExecutablePath point at it?",
                _options.PgDumpExecutablePath);
            return new PgDumpResult(false, $"pg_dump could not be started: {ex.Message}");
        }

        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var stderr = await stderrTask;
        await stdoutTask;

        if (process.ExitCode != 0)
        {
            _logger.LogError("pg_dump exited with code {ExitCode} for database '{Database}': {Error}", process.ExitCode, builder.Database, stderr);
            return new PgDumpResult(false, stderr);
        }

        return new PgDumpResult(true, null);
    }

    /// <summary>Pure construction, split out from <see cref="RunAsync"/> specifically so a unit
    /// test can assert the exact args/environment pg_dump receives without actually starting a
    /// process — most importantly, that the password only ever ends up in
    /// <see cref="ProcessStartInfo.Environment"/>, never <see cref="ProcessStartInfo.ArgumentList"/>.</summary>
    internal static ProcessStartInfo BuildStartInfo(NpgsqlConnectionStringBuilder connection, string pgDumpExecutablePath, string outputFilePath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = pgDumpExecutablePath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-h");
        startInfo.ArgumentList.Add(connection.Host ?? "localhost");
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add(connection.Port.ToString());
        startInfo.ArgumentList.Add("-U");
        startInfo.ArgumentList.Add(connection.Username ?? string.Empty);
        startInfo.ArgumentList.Add("-d");
        startInfo.ArgumentList.Add(connection.Database ?? string.Empty);
        // Custom format: compressed, and the only format pg_restore can selectively restore
        // from — the appropriate choice for "an admin downloads this and may need to restore it".
        startInfo.ArgumentList.Add("-Fc");
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add(outputFilePath);
        startInfo.Environment["PGPASSWORD"] = connection.Password ?? string.Empty;
        return startInfo;
    }
}
