namespace AssemblyToProcess.MultipleExports;

public class FooImporter
{
  public IReadOnlyList<IFooExporter> Imports { get; } =
    Vandelay.Importer.ImportMany<IFooExporter>(
      "AssemblyToProcess.MultipleExports2.dll");
}
