namespace HMS.Api.Configuration;

/// <summary>
/// The built React app this API serves (see <see cref="WebAppHostingConfiguration"/>), found
/// once at startup.
/// </summary>
/// <param name="RootPath">The folder holding the build: index.html, assets/ and so on.</param>
/// <param name="IndexFilePath">Its index.html, served for every app route (deep links, refreshes).</param>
/// <param name="ContentSecurityPolicy">The policy sent with the app's own pages, built from this index.html.</param>
public sealed record WebAppFiles(string RootPath, string IndexFilePath, string ContentSecurityPolicy);
