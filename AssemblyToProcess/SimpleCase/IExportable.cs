using AssemblyToProcess.SimpleCase;
using Vandelay;

[assembly: Exporter(typeof(IExportable))]

namespace AssemblyToProcess.SimpleCase;

public interface IExportable
{
}
