using System.Runtime.CompilerServices;

// Everything outside Contracts/ is internal to this module (docs/DeveloperHandbook.md §4).
// The unit test project is the one sanctioned friend assembly (mirrors HMS.Modules.Patients).
[assembly: InternalsVisibleTo("HMS.UnitTests")]
