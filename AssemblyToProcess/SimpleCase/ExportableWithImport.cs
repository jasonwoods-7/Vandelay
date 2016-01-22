using System.ComponentModel.Composition;

namespace AssemblyToProcess.SimpleCase
{
  public class ExportableWithImport : IExportable
  {
    [Import]
    public string Greeting { get; set; }
  }
}
