using Vandelay;

namespace AssemblyToProcess.SimpleCase;

public class ImporterEmptySearchPattern
{
  public IReadOnlyList<IExportable> Imports { get; } =
    Importer.ImportMany<IExportable>("");
}

public class ImporterSingleSearchPattern
{
  public IReadOnlyList<IExportable> Imports { get; } =
    Importer.ImportMany<IExportable>(
      "AssemblyToProcess.SimpleCase2.dll");
}

public class ImporterSingleSearchPatternWithImport
{
  public ImporterSingleSearchPatternWithImport()
  {
    var greetingExport = "Hello, World";

    Imports = Importer.ImportMany<IExportable>(
      "AssemblyToProcess.SimpleCase2.dll",
      greetingExport);
  }

  public IReadOnlyList<IExportable> Imports { get; }
}

public class ImporterMultipleSearchPatterns
{
  public IReadOnlyList<IExportable> Imports { get; } =
    Importer.ImportMany<IExportable>(
      "AssemblyToProcess.SimpleCase2.dll|" +
      "AssemblyToProcess.SimpleCase2.exe");
}

public class ImporterInheritsBase
{
  public IReadOnlyList<ExportBase> Imports { get; } =
    Importer.ImportMany<ExportBase>(
      "AssemblyToProcess.SimpleCase2.dll");
}

public class ImporterInheritedExport
{
  public IReadOnlyList<IInheritedExport> Imports { get; } =
    Importer.ImportMany<IInheritedExport>(
      "AssemblyToProcess.SimpleCase2.dll");
}
