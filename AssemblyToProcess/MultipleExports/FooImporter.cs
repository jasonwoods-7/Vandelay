using System.Collections.Generic;

namespace AssemblyToProcess.MultipleExports
{
  public class FooImporter
  {
    public IEnumerable<IFooExporter> Imports { get; } =
      Vandelay.Importer.ImportMany<IFooExporter>(
        "AssemblyToProcess.MultipleExports.dll");
  }
}
