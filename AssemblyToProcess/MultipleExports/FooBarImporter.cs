namespace AssemblyToProcess.MultipleExports
{
  public class FooBarImporter
  {
    public int IterateFooBars()
    {
      var count = 0;

      foreach (var _ in Vandelay.Importer.ImportMany<IFooExporter>(
        "AssemblyToProcess.MultipleExports.dll"))
      {
        ++count;
      }

      foreach (var _ in Vandelay.Importer.ImportMany<IBarExporter>(
        "AssemblyToProcess.MultipleExports.dll"))
      {
        ++count;
      }

      return count;
    }
  }
}
