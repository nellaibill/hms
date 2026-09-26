using System.Runtime.CompilerServices;

// Everything outside Contracts/ is internal to this module (docs/DeveloperHandbook.md §4).
// The unit test project is the one sanctioned friend assembly (mirrors HMS.Modules.Patients).
[assembly: InternalsVisibleTo("HMS.UnitTests")]

// NSubstitute (Castle.Core DynamicProxy) mocks internal interfaces such as
// IDocumentChunkRepository from this dynamically emitted assembly.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
