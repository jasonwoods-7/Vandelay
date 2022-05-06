using System.ComponentModel.Composition;
using AssemblyToProcess.Core;

namespace AssemblyToProcess.Unsigned;

[Export(typeof(IExportable))]
public class AlreadyExportedInstance : IExportable
{
}
