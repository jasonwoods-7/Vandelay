﻿using System.Collections.Generic;
using Vandelay;

namespace AssemblyToProcess.SimpleCase
{
  public class ImporterEmptySearchPattern
  {
    public IEnumerable<IExportable> Imports { get; } =
      Importer.ImportMany<IExportable>("");
  }

  public class ImporterSingleSearchPattern
  {
    public IEnumerable<IExportable> Imports { get; } =
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

    public IEnumerable<IExportable> Imports { get; }
  }

  public class ImporterMultipleSearchPatterns
  {
    public IEnumerable<IExportable> Imports { get; } =
      Importer.ImportMany<IExportable>(
        "AssemblyToProcess.SimpleCase2.dll|" +
        "AssemblyToProcess.SimpleCase2.exe");
  }

  public class ImporterInheritsBase
  {
    public IEnumerable<ExportBase> Imports { get; } =
      Importer.ImportMany<ExportBase>(
        "AssemblyToProcess.SimpleCase2.dll");
  }

  public class ImporterInheritedExport
  {
    public IEnumerable<IInheritedExport> Imports { get; } =
      Importer.ImportMany<IInheritedExport>(
        "AssemblyToProcess.SimpleCase2.dll");
  }
}
