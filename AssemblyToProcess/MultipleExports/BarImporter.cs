using System.Collections.Generic;

namespace AssemblyToProcess.MultipleExports
{
  public class BarImporter
  {
    public IEnumerable<IBarExporter> Imports { get; } =
      Vandelay.Importer.ImportMany<IBarExporter>(
        "AssemblyToProcess.MultipleExports.dll");
  }
}
