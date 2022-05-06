using AssemblyToProcess.MultipleExports;
using Vandelay;

[assembly: Exporter(typeof(IBarExporter))]

namespace AssemblyToProcess.MultipleExports;

public interface IBarExporter
{
}
