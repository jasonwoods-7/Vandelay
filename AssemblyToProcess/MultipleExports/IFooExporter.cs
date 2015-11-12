using AssemblyToProcess.MultipleExports;
using Vandelay;

[assembly: Exporter(typeof(IFooExporter))]

namespace AssemblyToProcess.MultipleExports
{
  public interface IFooExporter
  {
  }
}
