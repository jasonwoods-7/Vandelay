using System;
using Mono.Cecil;

namespace Vandelay.Fody
{
  public partial class ModuleWeaver
  {
    public Action<string> LogInfo { get; set; }
    public Action<string> LogWarn { get; set; }
    public Action<string> LogError { get; set; }

    public ModuleDefinition ModuleDefinition { get; set; }
    public IAssemblyResolver AssemblyResolver { get; set; }

    public ModuleWeaver()
    {
      LogInfo = _ => { };
      LogWarn = _ => { };
      LogError = _ => { };
    }

    public void Execute()
    {
      HandleExports();

      CleanReferences();
    }
  }
}
