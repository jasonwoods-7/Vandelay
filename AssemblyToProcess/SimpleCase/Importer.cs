using System.Collections.Generic;

namespace AssemblyToProcess.SimpleCase
{
  public class ImporterEmptySearchPattern
  {
    public IEnumerable<IExportable> Imports { get; } =
      Vandelay.Importer.ImportMany<IExportable>("");
  }

  public class ImporterSingleSearchPattern
  {
    public IEnumerable<IExportable> Imports { get; } =
      Vandelay.Importer.ImportMany<IExportable>("*2.dll");
  }

  public class ImporterSingleSearchPatternWithImport
  {
    public ImporterSingleSearchPatternWithImport()
    {
      var greetingExport = "Hello, World";

      Imports = Vandelay.Importer.ImportMany<IExportable>(
        "*2.dll", greetingExport);
    }

    public IEnumerable<IExportable> Imports { get; }
  }

  public class ImporterMultipleSearchPatterns
  {
    public IEnumerable<IExportable> Imports { get; } =
      Vandelay.Importer.ImportMany<IExportable>("*2.dll|*2.exe");
  }
}
