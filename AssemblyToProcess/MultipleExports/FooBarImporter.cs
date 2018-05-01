namespace AssemblyToProcess.MultipleExports
{
  public class FooBarImporter
  {
    public void IterateFooBars()
    {
      foreach (var _ in Vandelay.Importer.ImportMany<IFooExporter>(
        "AssemblyToProcess.MultipleExports2.dll"))
      {
      }

      foreach (var _ in Vandelay.Importer.ImportMany<IBarExporter>(
        "AssemblyToProcess.MultipleExports2.dll"))
      {
      }
    }
  }
}
