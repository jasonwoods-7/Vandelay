using Fody;
using JetBrains.Annotations;

namespace Vandelay.Fody
{
  [UsedImplicitly]
  public partial class ModuleWeaver : BaseModuleWeaver
  {
    [UsedImplicitly]
    public override void Execute()
    {
      FindReferences();

      HandleExports();
      HandleImports();

      CleanReferences();
    }
  }
}
