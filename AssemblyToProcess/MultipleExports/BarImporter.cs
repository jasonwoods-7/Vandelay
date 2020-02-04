using System.Collections.Generic;

namespace AssemblyToProcess.MultipleExports
{
  public class BarImporter
  {
    public IReadOnlyList<IBarExporter> Imports { get; } =
      Vandelay.Importer.ImportMany<IBarExporter>(
        "AssemblyToProcess.MultipleExports2.dll");
  }
}
