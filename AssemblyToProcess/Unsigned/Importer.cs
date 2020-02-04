using System.Collections.Generic;
using AssemblyToProcess.Core;

namespace AssemblyToProcess.Unsigned
{
  public class Importer
  {
    public IReadOnlyList<IExportable> Imports { get; } =
      Vandelay.Importer.ImportMany<IExportable>(
        "AssemblyToProcess.Signed2.dll|" +
        "AssemblyToProcess.Unsigned2.dll");
  }
}
