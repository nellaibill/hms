using System.Runtime.CompilerServices;

// Everything outside Contracts/ is internal to this module (docs/Architecture.md §4).
[assembly: InternalsVisibleTo("HMS.UnitTests")]

// NSubstitute (via Castle.Core's DynamicProxy) needs this to mock internal interfaces.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
