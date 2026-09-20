using System.Runtime.CompilerServices;

// Everything outside Contracts/ is internal to this module (docs/Architecture.md §4).
// The unit test project is the one sanctioned friend assembly.
[assembly: InternalsVisibleTo("HMS.UnitTests")]

// NSubstitute (via Castle.Core's DynamicProxy) needs this to mock internal interfaces.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
