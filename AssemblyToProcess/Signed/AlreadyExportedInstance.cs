using System.ComponentModel.Composition;
using AssemblyToProcess.Core;

namespace AssemblyToProcess.Signed;

[Export(typeof(IExportable))]
public class AlreadyExportedInstance : IExportable
{
}
