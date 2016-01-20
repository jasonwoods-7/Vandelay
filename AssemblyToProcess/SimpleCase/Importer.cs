using System.Collections.Generic;

namespace AssemblyToProcess.SimpleCase
{
  public class ImporterSingleSearchPattern
  {
    public IEnumerable<IExportable> Imports { get; } =
      Vandelay.Importer.ImportMany<IExportable>("*2.dll");
  }

  public class ImporterMultipleSearchPatterns
  {
    public IEnumerable<IExportable> Imports { get; } =
      Vandelay.Importer.ImportMany<IExportable>("*2.dll|*2.exe");
  }
}
