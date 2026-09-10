using FluentAssertions;
using HMS.Modules.Backups.Infrastructure;
using Npgsql;
using Xunit;

namespace HMS.UnitTests.Modules.Backups.Infrastructure;

public class PgDumpRunnerTests
{
    [Fact]
    public void BuildStartInfo_PassesConnectionDetailsAsArguments_AndPasswordOnlyViaEnvironment()
    {
        var connection = new NpgsqlConnectionStringBuilder("Host=db.internal;Port=5433;Database=hms_lhs;Username=hms_backup;Password=s3cret!");

        var startInfo = PgDumpRunner.BuildStartInfo(connection, "C:\\pg\\pg_dump.exe", "C:\\backups\\2026-09-10\\hms_lhs.dump");

        startInfo.FileName.Should().Be("C:\\pg\\pg_dump.exe");
        startInfo.ArgumentList.Should().ContainInOrder("-h", "db.internal", "-p", "5433", "-U", "hms_backup", "-d", "hms_lhs", "-Fc", "-f", "C:\\backups\\2026-09-10\\hms_lhs.dump");

        // The password must never appear in the argument list — argv is visible to every other
        // process on the box (Task Manager, `ps`, process-launch audit logs).
        startInfo.ArgumentList.Should().NotContain("s3cret!");
        startInfo.Environment["PGPASSWORD"].Should().Be("s3cret!");
    }

    [Fact]
    public void BuildStartInfo_DefaultsHostToLocalhost_WhenConnectionStringOmitsIt()
    {
        var connection = new NpgsqlConnectionStringBuilder("Database=hms_platform;Username=hms;Password=hms");

        var startInfo = PgDumpRunner.BuildStartInfo(connection, "pg_dump", "out.dump");

        startInfo.ArgumentList.Should().ContainInOrder("-h", "localhost");
    }

    [Fact]
    public void BuildStartInfo_RedirectsOutputAndDoesNotUseShellExecute()
    {
        var connection = new NpgsqlConnectionStringBuilder("Host=localhost;Database=hms_platform;Username=hms;Password=hms");

        var startInfo = PgDumpRunner.BuildStartInfo(connection, "pg_dump", "out.dump");

        startInfo.RedirectStandardError.Should().BeTrue();
        startInfo.RedirectStandardOutput.Should().BeTrue();
        startInfo.UseShellExecute.Should().BeFalse();
    }
}
