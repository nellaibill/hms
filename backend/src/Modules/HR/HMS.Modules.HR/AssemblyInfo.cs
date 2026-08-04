using System.Runtime.CompilerServices;

// Everything outside Contracts/ is internal to this module (docs/Architecture.md §4).
// The unit test project is the one sanctioned friend assembly, so tests can exercise
// Domain/Application/Infrastructure directly instead of only through the public API.
[assembly: InternalsVisibleTo("HMS.UnitTests")]

// NSubstitute (via Castle.Core's DynamicProxy) generates mock implementations of
// interfaces — including internal ones, like IShiftRepository — in a dynamically emitted
// assembly named "DynamicProxyGenAssembly2". Without this, Substitute.For<IShiftRepository>()
// throws ArgumentException at test run time ("...because it is not accessible...").
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
