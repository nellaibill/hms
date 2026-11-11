using System.Runtime.CompilerServices;

// Everything outside Contracts/ is internal to this module (docs/Architecture.md §4).
[assembly: InternalsVisibleTo("HMS.UnitTests")]

// The outbox/claim/receiver logic is mostly SQL, so it is also exercised against a real
// PostgreSQL container (Testcontainers) from the integration test project.
[assembly: InternalsVisibleTo("HMS.IntegrationTests")]

// NSubstitute (via Castle.Core's DynamicProxy) needs this to mock internal interfaces.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
