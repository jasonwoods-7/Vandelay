using AssemblyToProcess.SimpleCase;
using Vandelay;

[assembly: Exportable(typeof(IExportable))]

namespace AssemblyToProcess.SimpleCase
{
  public interface IExportable
  {
  }
}
