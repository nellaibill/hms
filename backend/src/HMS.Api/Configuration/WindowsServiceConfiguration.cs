namespace HMS.Api.Configuration;

/// <summary>
/// Lets the packaged Windows install run HMS.Api as a real Windows service (registered by the
/// installer, no NSSM wrapper), and keeps the content root on the install folder however the
/// process was started.
/// </summary>
public static class WindowsServiceConfiguration
{
    /// <summary>The Windows service name the packaged install registers.</summary>
    public const string ServiceName = "HMS";

    /// <summary>
    /// ASP.NET Core takes its content root (appsettings*.json, wwwroot, App_Data, webapp) from the
    /// current directory. Windows starts a service in C:\Windows\System32, and an operator may run
    /// <c>HMS.Api.exe migrate</c> from any folder, so both would look for every file in the wrong
    /// place. This moves to the folder the executable is in whenever the current directory has no
    /// appsettings.json. <c>dotnet run</c> (the project folder), Docker (WORKDIR /app) and the NSSM
    /// install (AppDirectory = the publish folder) all start in a folder that has one, so they are
    /// unchanged. Must run before <c>WebApplication.CreateBuilder</c>.
    /// </summary>
    public static void UseExecutableFolderWhenStartedElsewhere()
    {
        if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")))
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        }
    }

    /// <summary>
    /// When the process runs as a Windows service, reports start and stop to the Service Control
    /// Manager and also logs warnings and errors to the Windows Event Log. Does nothing when it runs
    /// any other way (console, Docker, Linux).
    /// </summary>
    public static IServiceCollection AddHmsWindowsService(this IServiceCollection services)
        => services.AddWindowsService(options => options.ServiceName = ServiceName);
}
