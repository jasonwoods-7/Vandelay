using System;
using JetBrains.Annotations;
using Mono.Cecil;

namespace Vandelay.Fody
{
  [UsedImplicitly]
  public partial class ModuleWeaver
  {
    [UsedImplicitly, NotNull]
    public Action<string> LogInfo { get; set; }

    [UsedImplicitly, NotNull]
    public Action<string> LogWarn { get; set; }

    [UsedImplicitly, NotNull]
    public Action<string> LogError { get; set; }

    [UsedImplicitly, NotNull]
    public ModuleDefinition ModuleDefinition { get; set; }

    [UsedImplicitly, NotNull]
    public IAssemblyResolver AssemblyResolver { get; set; }

    // ReSharper disable once NotNullMemberIsNotInitialized
    public ModuleWeaver()
    {
      LogInfo = _ => { };
      LogWarn = _ => { };
      LogError = _ => { };
    }

    [UsedImplicitly]
    public void Execute()
    {
      ReferenceFinder.SetModule(ModuleDefinition);
      ReferenceFinder.FindReferences(AssemblyResolver);

      HandleExports();
      HandleImports();

      CleanReferences();
    }
  }
}
