using System.Collections.Generic;
using AssemblyToProcess.Core;

namespace AssemblyToProcess.Signed
{
  public class Importer
  {
    public IEnumerable<IExportable> Imports { get; } =
      Vandelay.Importer.ImportMany<IExportable>(
        "AssemblyToProcess.Signed.dll|" +
        "AssemblyToProcess.Unsigned.dll");
  }
}
