using System.Runtime.CompilerServices;

// Everything outside Contracts/ is internal to this module (docs/DeveloperHandbook.md §4).
// The unit test project is the one sanctioned friend assembly.
[assembly: InternalsVisibleTo("HMS.UnitTests")]

// NSubstitute (via Castle.Core's DynamicProxy) generates mock implementations of
// interfaces — including internal ones — in a dynamically emitted assembly named
// "DynamicProxyGenAssembly2". Without this, Substitute.For<...>() throws at test run time.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
