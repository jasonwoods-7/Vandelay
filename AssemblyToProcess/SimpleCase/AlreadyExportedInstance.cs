using System.ComponentModel.Composition;

namespace AssemblyToProcess.SimpleCase;

[Export(typeof(IExportable))]
public class AlreadyExportedInstance : IExportable
{
}
